import React, { useState, useEffect } from 'react';
import { supabase } from '../lib/supabaseClient';

const SupabaseTest: React.FC = () => {
  const [connectionStatus, setConnectionStatus] = useState<'testing' | 'connected' | 'error'>('testing');
  const [error, setError] = useState<string>('');

  useEffect(() => {
    testConnection();
  }, []);

  const testConnection = async () => {
    try {
      // Test basic Supabase connection with auth
      const { data: { session }, error } = await supabase.auth.getSession();
      
      if (error && error.message !== 'No session') {
        throw error;
      }

      // If we get here, Supabase is connected (session can be null for anonymous users)
      setConnectionStatus('connected');
      console.log('✅ Supabase connection successful');
    } catch (err: any) {
      setConnectionStatus('error');
      setError(err.message);
      console.error('❌ Supabase connection failed:', err);
    }
  };

  return (
    <div style={{ 
      position: 'fixed', 
      bottom: '20px', 
      right: '20px', 
      padding: '10px', 
      borderRadius: '8px',
      fontSize: '12px',
      zIndex: 1000,
      backgroundColor: connectionStatus === 'connected' ? '#10b981' : connectionStatus === 'error' ? '#ef4444' : '#f59e0b',
      color: 'white'
    }}>
      {connectionStatus === 'testing' && '🔄 Testing Supabase...'}
      {connectionStatus === 'connected' && '✅ Supabase Connected'}
      {connectionStatus === 'error' && `❌ Supabase Error: ${error}`}
    </div>
  );
};

export default SupabaseTest;
