-- ============================================================================
-- CAD Editor Supabase Integration - SQL Editor Code
-- ============================================================================
-- This file contains all the SQL code for the CAD Editor application
-- including database schema, functions, triggers, and RLS policies

-- ============================================================================
-- 1. ENABLE EXTENSIONS
-- ============================================================================

-- Enable necessary extensions for our CAD application
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "postgis"; -- For geometric operations
CREATE EXTENSION IF NOT EXISTS "pg_trgm"; -- For text search

-- ============================================================================
-- 2. DATABASE SCHEMA
-- ============================================================================

-- Users table (extends Supabase auth.users)
CREATE TABLE IF NOT EXISTS public.profiles (
    id UUID REFERENCES auth.users(id) PRIMARY KEY,
    username TEXT UNIQUE,
    full_name TEXT,
    avatar_url TEXT,
    website TEXT,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT TIMEZONE('utc'::text, NOW()) NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT TIMEZONE('utc'::text, NOW()) NOT NULL
);

-- CAD Projects table
CREATE TABLE IF NOT EXISTS public.cad_projects (
    id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    user_id UUID REFERENCES public.profiles(id) ON DELETE CASCADE NOT NULL,
    name TEXT NOT NULL,
    description TEXT,
    thumbnail_url TEXT,
    is_public BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT TIMEZONE('utc'::text, NOW()) NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT TIMEZONE('utc'::text, NOW()) NOT NULL
);

-- CAD Elements table (stores individual drawing elements)
CREATE TABLE IF NOT EXISTS public.cad_elements (
    id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    project_id UUID REFERENCES public.cad_projects(id) ON DELETE CASCADE NOT NULL,
    element_type TEXT NOT NULL CHECK (element_type IN ('line', 'circle', 'rectangle', 'polygon', 'text', 'dimension')),
    geometry JSONB NOT NULL, -- Stores geometric data (coordinates, dimensions, etc.)
    style JSONB DEFAULT '{}', -- Stores styling information (color, line weight, etc.)
    layer TEXT DEFAULT 'default',
    locked BOOLEAN DEFAULT false,
    visible BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT TIMEZONE('utc'::text, NOW()) NOT NULL,
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT TIMEZONE('utc'::text, NOW()) NOT NULL
);

-- CAD Layers table
CREATE TABLE IF NOT EXISTS public.cad_layers (
    id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    project_id UUID REFERENCES public.cad_projects(id) ON DELETE CASCADE NOT NULL,
    name TEXT NOT NULL,
    color TEXT DEFAULT '#000000',
    line_weight INTEGER DEFAULT 1,
    visible BOOLEAN DEFAULT true,
    locked BOOLEAN DEFAULT false,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT TIMEZONE('utc'::text, NOW()) NOT NULL,
    UNIQUE(project_id, name)
);

-- Project Collaborators table
CREATE TABLE IF NOT EXISTS public.project_collaborators (
    id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    project_id UUID REFERENCES public.cad_projects(id) ON DELETE CASCADE NOT NULL,
    user_id UUID REFERENCES public.profiles(id) ON DELETE CASCADE NOT NULL,
    permission_level TEXT CHECK (permission_level IN ('view', 'edit', 'admin')) DEFAULT 'view',
    invited_at TIMESTAMP WITH TIME ZONE DEFAULT TIMEZONE('utc'::text, NOW()) NOT NULL,
    UNIQUE(project_id, user_id)
);

-- Project Versions/History table
CREATE TABLE IF NOT EXISTS public.project_versions (
    id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    project_id UUID REFERENCES public.cad_projects(id) ON DELETE CASCADE NOT NULL,
    version_number INTEGER NOT NULL,
    description TEXT,
    data JSONB NOT NULL, -- Complete project data snapshot
    created_by UUID REFERENCES public.profiles(id) ON DELETE SET NULL,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT TIMEZONE('utc'::text, NOW()) NOT NULL,
    UNIQUE(project_id, version_number)
);

-- ============================================================================
-- 3. INDEXES FOR PERFORMANCE
-- ============================================================================

-- Indexes for better query performance
CREATE INDEX IF NOT EXISTS idx_cad_projects_user_id ON public.cad_projects(user_id);
CREATE INDEX IF NOT EXISTS idx_cad_projects_created_at ON public.cad_projects(created_at);
CREATE INDEX IF NOT EXISTS idx_cad_elements_project_id ON public.cad_elements(project_id);
CREATE INDEX IF NOT EXISTS idx_cad_elements_element_type ON public.cad_elements(element_type);
CREATE INDEX IF NOT EXISTS idx_cad_elements_layer ON public.cad_elements(layer);
CREATE INDEX IF NOT EXISTS idx_cad_layers_project_id ON public.cad_layers(project_id);
CREATE INDEX IF NOT EXISTS idx_project_collaborators_project_id ON public.project_collaborators(project_id);
CREATE INDEX IF NOT EXISTS idx_project_collaborators_user_id ON public.project_collaborators(user_id);
CREATE INDEX IF NOT EXISTS idx_project_versions_project_id ON public.project_versions(project_id);

-- Text search indexes
CREATE INDEX IF NOT EXISTS idx_cad_projects_name_trgm ON public.cad_projects USING gin (name gin_trgm_ops);
CREATE INDEX IF NOT EXISTS idx_profiles_username_trgm ON public.profiles USING gin (username gin_trgm_ops);

-- ============================================================================
-- 4. DATABASE FUNCTIONS
-- ============================================================================

-- Function to update updated_at timestamp
CREATE OR REPLACE FUNCTION update_updated_at_column()
RETURNS TRIGGER AS $$
BEGIN
    NEW.updated_at = TIMEZONE('utc'::text, NOW());
    RETURN NEW;
END;
$$ language 'plpgsql';

-- Function to create a new project version
CREATE OR REPLACE FUNCTION create_project_version(
    p_project_id UUID,
    p_description TEXT DEFAULT NULL
)
RETURNS UUID AS $$
DECLARE
    v_version_number INTEGER;
    v_project_data JSONB;
    v_version_id UUID;
BEGIN
    -- Get the next version number
    SELECT COALESCE(MAX(version_number), 0) + 1 
    INTO v_version_number
    FROM public.project_versions 
    WHERE project_id = p_project_id;
    
    -- Get current project data
    SELECT jsonb_build_object(
        'project', row_to_json(p.*),
        'elements', COALESCE(
            (SELECT jsonb_agg(row_to_json(e.*))
             FROM public.cad_elements e 
             WHERE e.project_id = p_project_id), '[]'::jsonb
        ),
        'layers', COALESCE(
            (SELECT jsonb_agg(row_to_json(l.*))
             FROM public.cad_layers l 
             WHERE l.project_id = p_project_id), '[]'::jsonb
        )
    )
    INTO v_project_data
    FROM public.cad_projects p
    WHERE p.id = p_project_id;
    
    -- Insert new version
    INSERT INTO public.project_versions (project_id, version_number, description, data, created_by)
    VALUES (p_project_id, v_version_number, p_description, v_project_data, auth.uid())
    RETURNING id INTO v_version_id;
    
    RETURN v_version_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to duplicate a project
CREATE OR REPLACE FUNCTION duplicate_project(
    p_project_id UUID,
    p_new_name TEXT
)
RETURNS UUID AS $$
DECLARE
    v_new_project_id UUID;
    v_element RECORD;
    v_layer RECORD;
BEGIN
    -- Create new project
    INSERT INTO public.cad_projects (user_id, name, description, is_public)
    SELECT auth.uid(), p_new_name, description || ' (Copy)', false
    FROM public.cad_projects
    WHERE id = p_project_id
    RETURNING id INTO v_new_project_id;
    
    -- Copy layers
    FOR v_layer IN 
        SELECT * FROM public.cad_layers WHERE project_id = p_project_id
    LOOP
        INSERT INTO public.cad_layers (project_id, name, color, line_weight, visible, locked)
        VALUES (v_new_project_id, v_layer.name, v_layer.color, v_layer.line_weight, v_layer.visible, v_layer.locked);
    END LOOP;
    
    -- Copy elements
    FOR v_element IN 
        SELECT * FROM public.cad_elements WHERE project_id = p_project_id
    LOOP
        INSERT INTO public.cad_elements (project_id, element_type, geometry, style, layer, locked, visible)
        VALUES (v_new_project_id, v_element.element_type, v_element.geometry, v_element.style, v_element.layer, v_element.locked, v_element.visible);
    END LOOP;
    
    RETURN v_new_project_id;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Function to get project statistics
CREATE OR REPLACE FUNCTION get_project_stats(p_project_id UUID)
RETURNS JSONB AS $$
DECLARE
    v_stats JSONB;
BEGIN
    SELECT jsonb_build_object(
        'total_elements', COUNT(*),
        'elements_by_type', jsonb_object_agg(element_type, type_count),
        'layers_count', (
            SELECT COUNT(*) 
            FROM public.cad_layers 
            WHERE project_id = p_project_id
        ),
        'last_modified', MAX(updated_at)
    )
    INTO v_stats
    FROM (
        SELECT 
            element_type,
            COUNT(*) as type_count,
            MAX(updated_at) as updated_at
        FROM public.cad_elements 
        WHERE project_id = p_project_id
        GROUP BY element_type
    ) t;
    
    RETURN COALESCE(v_stats, '{}'::jsonb);
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- ============================================================================
-- 5. TRIGGERS
-- ============================================================================

-- Triggers to automatically update updated_at timestamps
CREATE TRIGGER update_profiles_updated_at 
    BEFORE UPDATE ON public.profiles 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_cad_projects_updated_at 
    BEFORE UPDATE ON public.cad_projects 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

CREATE TRIGGER update_cad_elements_updated_at 
    BEFORE UPDATE ON public.cad_elements 
    FOR EACH ROW EXECUTE FUNCTION update_updated_at_column();

-- Trigger to create default layers when a new project is created
CREATE OR REPLACE FUNCTION create_default_layers()
RETURNS TRIGGER AS $$
BEGIN
    -- Create default layer
    INSERT INTO public.cad_layers (project_id, name, color, line_weight)
    VALUES (NEW.id, 'Default', '#000000', 1);
    
    -- Create construction layer
    INSERT INTO public.cad_layers (project_id, name, color, line_weight)
    VALUES (NEW.id, 'Construction', '#CCCCCC', 1);
    
    -- Create dimensions layer
    INSERT INTO public.cad_layers (project_id, name, color, line_weight)
    VALUES (NEW.id, 'Dimensions', '#FF0000', 1);
    
    RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER create_default_layers_trigger
    AFTER INSERT ON public.cad_projects
    FOR EACH ROW EXECUTE FUNCTION create_default_layers();

-- ============================================================================
-- 6. ROW LEVEL SECURITY (RLS) POLICIES
-- ============================================================================

-- Enable RLS on all tables
ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.cad_projects ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.cad_elements ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.cad_layers ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.project_collaborators ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.project_versions ENABLE ROW LEVEL SECURITY;

-- Profiles policies
CREATE POLICY "Public profiles are viewable by everyone" ON public.profiles
    FOR SELECT USING (true);

CREATE POLICY "Users can insert their own profile" ON public.profiles
    FOR INSERT WITH CHECK (auth.uid() = id);

CREATE POLICY "Users can update own profile" ON public.profiles
    FOR UPDATE USING (auth.uid() = id);

-- CAD Projects policies
CREATE POLICY "Users can view their own projects" ON public.cad_projects
    FOR SELECT USING (
        auth.uid() = user_id OR 
        is_public = true OR
        EXISTS (
            SELECT 1 FROM public.project_collaborators 
            WHERE project_id = id AND user_id = auth.uid()
        )
    );

CREATE POLICY "Users can create their own projects" ON public.cad_projects
    FOR INSERT WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update their own projects or projects they have edit access to" ON public.cad_projects
    FOR UPDATE USING (
        auth.uid() = user_id OR
        EXISTS (
            SELECT 1 FROM public.project_collaborators 
            WHERE project_id = id AND user_id = auth.uid() 
            AND permission_level IN ('edit', 'admin')
        )
    );

CREATE POLICY "Users can delete their own projects" ON public.cad_projects
    FOR DELETE USING (auth.uid() = user_id);

-- CAD Elements policies
CREATE POLICY "Users can view elements of accessible projects" ON public.cad_elements
    FOR SELECT USING (
        EXISTS (
            SELECT 1 FROM public.cad_projects p
            WHERE p.id = project_id AND (
                p.user_id = auth.uid() OR 
                p.is_public = true OR
                EXISTS (
                    SELECT 1 FROM public.project_collaborators 
                    WHERE project_id = p.id AND user_id = auth.uid()
                )
            )
        )
    );

CREATE POLICY "Users can modify elements of projects they have edit access to" ON public.cad_elements
    FOR ALL USING (
        EXISTS (
            SELECT 1 FROM public.cad_projects p
            WHERE p.id = project_id AND (
                p.user_id = auth.uid() OR
                EXISTS (
                    SELECT 1 FROM public.project_collaborators 
                    WHERE project_id = p.id AND user_id = auth.uid() 
                    AND permission_level IN ('edit', 'admin')
                )
            )
        )
    );

-- CAD Layers policies (similar to elements)
CREATE POLICY "Users can view layers of accessible projects" ON public.cad_layers
    FOR SELECT USING (
        EXISTS (
            SELECT 1 FROM public.cad_projects p
            WHERE p.id = project_id AND (
                p.user_id = auth.uid() OR 
                p.is_public = true OR
                EXISTS (
                    SELECT 1 FROM public.project_collaborators 
                    WHERE project_id = p.id AND user_id = auth.uid()
                )
            )
        )
    );

CREATE POLICY "Users can modify layers of projects they have edit access to" ON public.cad_layers
    FOR ALL USING (
        EXISTS (
            SELECT 1 FROM public.cad_projects p
            WHERE p.id = project_id AND (
                p.user_id = auth.uid() OR
                EXISTS (
                    SELECT 1 FROM public.project_collaborators 
                    WHERE project_id = p.id AND user_id = auth.uid() 
                    AND permission_level IN ('edit', 'admin')
                )
            )
        )
    );

-- Project Collaborators policies
CREATE POLICY "Project owners and admins can manage collaborators" ON public.project_collaborators
    FOR ALL USING (
        EXISTS (
            SELECT 1 FROM public.cad_projects p
            WHERE p.id = project_id AND (
                p.user_id = auth.uid() OR
                EXISTS (
                    SELECT 1 FROM public.project_collaborators pc
                    WHERE pc.project_id = p.id AND pc.user_id = auth.uid() 
                    AND pc.permission_level = 'admin'
                )
            )
        )
    );

-- Project Versions policies
CREATE POLICY "Users can view versions of accessible projects" ON public.project_versions
    FOR SELECT USING (
        EXISTS (
            SELECT 1 FROM public.cad_projects p
            WHERE p.id = project_id AND (
                p.user_id = auth.uid() OR 
                p.is_public = true OR
                EXISTS (
                    SELECT 1 FROM public.project_collaborators 
                    WHERE project_id = p.id AND user_id = auth.uid()
                )
            )
        )
    );

CREATE POLICY "Users can create versions for projects they have edit access to" ON public.project_versions
    FOR INSERT WITH CHECK (
        EXISTS (
            SELECT 1 FROM public.cad_projects p
            WHERE p.id = project_id AND (
                p.user_id = auth.uid() OR
                EXISTS (
                    SELECT 1 FROM public.project_collaborators 
                    WHERE project_id = p.id AND user_id = auth.uid() 
                    AND permission_level IN ('edit', 'admin')
                )
            )
        )
    );

-- ============================================================================
-- 7. SAMPLE DATA (Optional - for testing)
-- ============================================================================

-- Uncomment the following to insert sample data for testing
/*
-- Insert sample profile (this would normally be created when a user signs up)
INSERT INTO public.profiles (id, username, full_name, avatar_url)
VALUES (
    '00000000-0000-0000-0000-000000000001',
    'testuser',
    'Test User',
    'https://example.com/avatar.jpg'
) ON CONFLICT (id) DO NOTHING;

-- Insert sample project
INSERT INTO public.cad_projects (id, user_id, name, description, is_public)
VALUES (
    '10000000-0000-0000-0000-000000000001',
    '00000000-0000-0000-0000-000000000001',
    'Sample CAD Project',
    'A sample project for testing',
    true
) ON CONFLICT (id) DO NOTHING;

-- Sample elements will be created through the application
*/

-- ============================================================================
-- 8. USEFUL QUERIES FOR THE APPLICATION
-- ============================================================================

/*
-- Get all projects for a user with statistics
SELECT 
    p.*,
    (SELECT COUNT(*) FROM public.cad_elements WHERE project_id = p.id) as element_count,
    (SELECT COUNT(*) FROM public.cad_layers WHERE project_id = p.id) as layer_count
FROM public.cad_projects p
WHERE p.user_id = auth.uid()
ORDER BY p.updated_at DESC;

-- Get project with all elements and layers
SELECT 
    p.*,
    COALESCE(
        json_agg(DISTINCT e.*) FILTER (WHERE e.id IS NOT NULL), 
        '[]'
    ) as elements,
    COALESCE(
        json_agg(DISTINCT l.*) FILTER (WHERE l.id IS NOT NULL), 
        '[]'
    ) as layers
FROM public.cad_projects p
LEFT JOIN public.cad_elements e ON e.project_id = p.id
LEFT JOIN public.cad_layers l ON l.project_id = p.id
WHERE p.id = $1
GROUP BY p.id;

-- Search projects by name
SELECT p.*, 
       similarity(p.name, $1) as similarity_score
FROM public.cad_projects p
WHERE p.name % $1
   OR p.description % $1
ORDER BY similarity_score DESC;

-- Get project collaborators with user details
SELECT 
    pc.*,
    pr.username,
    pr.full_name,
    pr.avatar_url
FROM public.project_collaborators pc
JOIN public.profiles pr ON pr.id = pc.user_id
WHERE pc.project_id = $1;
*/

-- ============================================================================
-- END OF SQL EDITOR INTEGRATION CODE
-- ============================================================================
