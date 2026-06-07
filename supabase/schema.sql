-- HoloDraft Database Schema
-- Run this in your Supabase SQL editor to set up the database

-- Enable necessary extensions
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Users table (extends Supabase auth.users)
CREATE TABLE public.holodraft_users (
    id UUID REFERENCES auth.users(id) PRIMARY KEY,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    email TEXT NOT NULL,
    full_name TEXT,
    avatar_url TEXT,
    subscription_type TEXT DEFAULT 'free' CHECK (subscription_type IN ('free', 'pro', 'enterprise')),
    total_files_uploaded INTEGER DEFAULT 0,
    total_storage_used BIGINT DEFAULT 0, -- in bytes
    last_login TIMESTAMP WITH TIME ZONE
);

-- Files table
CREATE TABLE public.holodraft_files (
    id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    user_id UUID REFERENCES public.holodraft_users(id) ON DELETE CASCADE,
    original_name TEXT NOT NULL,
    file_size BIGINT NOT NULL,
    original_format TEXT NOT NULL,
    status TEXT DEFAULT 'uploaded' CHECK (status IN ('uploading', 'uploaded', 'converting', 'converted', 'error')),
    original_url TEXT,
    converted_url TEXT,
    error_message TEXT,
    conversion_started_at TIMESTAMP WITH TIME ZONE,
    conversion_completed_at TIMESTAMP WITH TIME ZONE,
    metadata JSONB DEFAULT '{}', -- Additional file metadata
    tags TEXT[] DEFAULT '{}', -- User-defined tags
    is_public BOOLEAN DEFAULT FALSE,
    download_count INTEGER DEFAULT 0
);

-- Analytics table
CREATE TABLE public.holodraft_analytics (
    id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    user_id UUID REFERENCES public.holodraft_users(id) ON DELETE CASCADE,
    event_type TEXT NOT NULL, -- 'file_upload', 'file_conversion', 'ar_view', etc.
    metadata JSONB DEFAULT '{}', -- Event-specific data
    session_id TEXT, -- For grouping related events
    ip_address INET,
    user_agent TEXT
);

-- Conversion jobs table (for background processing)
CREATE TABLE public.holodraft_conversion_jobs (
    id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    file_id UUID REFERENCES public.holodraft_files(id) ON DELETE CASCADE,
    user_id UUID REFERENCES public.holodraft_users(id) ON DELETE CASCADE,
    status TEXT DEFAULT 'pending' CHECK (status IN ('pending', 'processing', 'completed', 'failed')),
    input_format TEXT NOT NULL,
    output_format TEXT NOT NULL DEFAULT 'fbx',
    priority INTEGER DEFAULT 5, -- 1-10, higher = more priority
    attempts INTEGER DEFAULT 0,
    max_attempts INTEGER DEFAULT 3,
    error_log TEXT,
    processing_time_ms INTEGER,
    worker_id TEXT
);

-- Shared projects table (for collaboration)
CREATE TABLE public.holodraft_shared_projects (
    id UUID DEFAULT uuid_generate_v4() PRIMARY KEY,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    owner_id UUID REFERENCES public.holodraft_users(id) ON DELETE CASCADE,
    name TEXT NOT NULL,
    description TEXT,
    is_public BOOLEAN DEFAULT FALSE,
    access_code TEXT UNIQUE, -- For easy sharing
    max_collaborators INTEGER DEFAULT 5
);

-- Project files junction table
CREATE TABLE public.holodraft_project_files (
    project_id UUID REFERENCES public.holodraft_shared_projects(id) ON DELETE CASCADE,
    file_id UUID REFERENCES public.holodraft_files(id) ON DELETE CASCADE,
    added_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    added_by UUID REFERENCES public.holodraft_users(id) ON DELETE CASCADE,
    PRIMARY KEY (project_id, file_id)
);

-- Project collaborators table
CREATE TABLE public.holodraft_project_collaborators (
    project_id UUID REFERENCES public.holodraft_shared_projects(id) ON DELETE CASCADE,
    user_id UUID REFERENCES public.holodraft_users(id) ON DELETE CASCADE,
    role TEXT DEFAULT 'viewer' CHECK (role IN ('owner', 'editor', 'viewer')),
    joined_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    PRIMARY KEY (project_id, user_id)
);

-- Create indexes for better performance
CREATE INDEX idx_holodraft_files_user_id ON public.holodraft_files(user_id);
CREATE INDEX idx_holodraft_files_status ON public.holodraft_files(status);
CREATE INDEX idx_holodraft_files_created_at ON public.holodraft_files(created_at DESC);
CREATE INDEX idx_holodraft_analytics_user_id ON public.holodraft_analytics(user_id);
CREATE INDEX idx_holodraft_analytics_event_type ON public.holodraft_analytics(event_type);
CREATE INDEX idx_holodraft_conversion_jobs_status ON public.holodraft_conversion_jobs(status);
CREATE INDEX idx_holodraft_conversion_jobs_priority ON public.holodraft_conversion_jobs(priority DESC);

-- Row Level Security (RLS) policies
ALTER TABLE public.holodraft_users ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.holodraft_files ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.holodraft_analytics ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.holodraft_conversion_jobs ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.holodraft_shared_projects ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.holodraft_project_files ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.holodraft_project_collaborators ENABLE ROW LEVEL SECURITY;

-- Users can only see their own data
CREATE POLICY "Users can view own profile" ON public.holodraft_users
    FOR SELECT USING (auth.uid() = id);

CREATE POLICY "Users can update own profile" ON public.holodraft_users
    FOR UPDATE USING (auth.uid() = id);

-- Files policies
CREATE POLICY "Users can view own files" ON public.holodraft_files
    FOR SELECT USING (auth.uid() = user_id OR is_public = TRUE);

CREATE POLICY "Users can insert own files" ON public.holodraft_files
    FOR INSERT WITH CHECK (auth.uid() = user_id);

CREATE POLICY "Users can update own files" ON public.holodraft_files
    FOR UPDATE USING (auth.uid() = user_id);

CREATE POLICY "Users can delete own files" ON public.holodraft_files
    FOR DELETE USING (auth.uid() = user_id);

-- Analytics policies
CREATE POLICY "Users can view own analytics" ON public.holodraft_analytics
    FOR SELECT USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own analytics" ON public.holodraft_analytics
    FOR INSERT WITH CHECK (auth.uid() = user_id);

-- Conversion jobs policies
CREATE POLICY "Users can view own conversion jobs" ON public.holodraft_conversion_jobs
    FOR SELECT USING (auth.uid() = user_id);

CREATE POLICY "Users can insert own conversion jobs" ON public.holodraft_conversion_jobs
    FOR INSERT WITH CHECK (auth.uid() = user_id);

-- Functions for automatic user creation
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER AS $$
BEGIN
    INSERT INTO public.holodraft_users (id, email, full_name, avatar_url)
    VALUES (
        NEW.id,
        NEW.email,
        NEW.raw_user_meta_data->>'full_name',
        NEW.raw_user_meta_data->>'avatar_url'
    );
    RETURN NEW;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Trigger to create user profile on signup
CREATE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW EXECUTE FUNCTION public.handle_new_user();

-- Function to update user stats
CREATE OR REPLACE FUNCTION public.update_user_stats()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' THEN
        UPDATE public.holodraft_users
        SET 
            total_files_uploaded = total_files_uploaded + 1,
            total_storage_used = total_storage_used + NEW.file_size
        WHERE id = NEW.user_id;
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE public.holodraft_users
        SET 
            total_files_uploaded = total_files_uploaded - 1,
            total_storage_used = total_storage_used - OLD.file_size
        WHERE id = OLD.user_id;
        RETURN OLD;
    END IF;
    RETURN NULL;
END;
$$ LANGUAGE plpgsql SECURITY DEFINER;

-- Trigger to update user stats when files are added/removed
CREATE TRIGGER on_file_stats_change
    AFTER INSERT OR DELETE ON public.holodraft_files
    FOR EACH ROW EXECUTE FUNCTION public.update_user_stats();

-- Storage bucket for CAD files
INSERT INTO storage.buckets (id, name, public)
VALUES ('cad-files', 'cad-files', true);

-- Storage policies
CREATE POLICY "Users can upload their own files" ON storage.objects
    FOR INSERT WITH CHECK (bucket_id = 'cad-files' AND auth.uid()::text = (storage.foldername(name))[1]);

CREATE POLICY "Users can view their own files" ON storage.objects
    FOR SELECT USING (bucket_id = 'cad-files' AND auth.uid()::text = (storage.foldername(name))[1]);

CREATE POLICY "Users can delete their own files" ON storage.objects
    FOR DELETE USING (bucket_id = 'cad-files' AND auth.uid()::text = (storage.foldername(name))[1]);

-- Create a view for file statistics
CREATE VIEW public.holodraft_file_stats AS
SELECT 
    user_id,
    COUNT(*) as total_files,
    SUM(file_size) as total_size,
    COUNT(CASE WHEN status = 'converted' THEN 1 END) as converted_files,
    COUNT(CASE WHEN status = 'error' THEN 1 END) as failed_files,
    AVG(CASE 
        WHEN conversion_completed_at IS NOT NULL AND conversion_started_at IS NOT NULL 
        THEN EXTRACT(EPOCH FROM (conversion_completed_at - conversion_started_at)) * 1000 
    END) as avg_conversion_time_ms
FROM public.holodraft_files
GROUP BY user_id;

-- Grant access to the view
GRANT SELECT ON public.holodraft_file_stats TO authenticated;
