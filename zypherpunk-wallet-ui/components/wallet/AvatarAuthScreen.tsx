'use client';

import React, { useState } from 'react';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Loader2 } from 'lucide-react';
import { useAvatarStore } from '@/lib/avatarStore';

type Mode = 'login' | 'register';

export const AvatarAuthScreen: React.FC = () => {
  const { login, register, isAuthenticating, authError, useDemoAvatar } = useAvatarStore();
  const [mode, setMode] = useState<Mode>('login');
  const [form, setForm] = useState({
    username: '',
    email: '',
    password: '',
    firstName: '',
    lastName: '',
  });
  const [localError, setLocalError] = useState<string | null>(null);

  const updateField = (field: string, value: string) => {
    setForm((prev) => ({ ...prev, [field]: value }));
    setLocalError(null);
  };

  const handleSubmit = async (event: React.FormEvent) => {
    event.preventDefault();
    setLocalError(null);

    try {
      if (mode === 'login') {
        if (!form.username || !form.password) {
          setLocalError('Enter your username/email and password.');
          return;
        }
        await login(form.username.trim(), form.password);
      } else {
        if (!form.username || !form.email || !form.password) {
          setLocalError('Username, email, and password are required.');
          return;
        }
        await register({
          username: form.username.trim(),
          email: form.email.trim(),
          password: form.password,
          firstName: form.firstName.trim() || undefined,
          lastName: form.lastName.trim() || undefined,
        });
      }
    } catch {
      // errors handled in store
    }
  };

  return (
    <div className="min-h-screen w-full bg-black text-white flex items-center justify-center px-4 py-6">
      <div className="w-full max-w-md bg-gradient-to-b from-gray-900/90 to-black border border-white/10 rounded-3xl p-8 shadow-2xl space-y-6">
        <div className="flex flex-col items-center gap-3">
          <div
            className="h-16 w-16 rounded-full border border-purple-400/40 bg-gradient-to-br from-purple-700 via-indigo-600 to-black flex items-center justify-center animate-spin"
            style={{ animationDuration: '4s' }}
          >
            <div className="h-10 w-10 rounded-full border border-white/30 flex items-center justify-center text-lg font-semibold">
              O
            </div>
          </div>
          <p className="text-xs uppercase tracking-[0.35em] text-gray-400">Avatar Access</p>
        </div>
        <div className="text-center mb-8">
          <div className="text-sm uppercase tracking-[0.3em] text-gray-400 mb-4">Connect Identity</div>
          <h1 className="text-3xl font-semibold">Connect your avatar</h1>
          <p className="text-gray-400 mt-2">
            Sign in with your OASIS avatar to load your multi-chain wallets.
          </p>
        </div>

        <div className="flex mb-6 rounded-full bg-white/5 border border-white/5 p-1">
          <button
            onClick={() => setMode('login')}
            className={`flex-1 py-2 rounded-full text-sm font-medium transition-colors ${
              mode === 'login' ? 'bg-white text-black' : 'text-gray-400'
            }`}
            disabled={isAuthenticating}
          >
            Sign in
          </button>
          <button
            onClick={() => setMode('register')}
            className={`flex-1 py-2 rounded-full text-sm font-medium transition-colors ${
              mode === 'register' ? 'bg-white text-black' : 'text-gray-400'
            }`}
            disabled={isAuthenticating}
          >
            Create avatar
          </button>
        </div>

        <form className="space-y-4" onSubmit={handleSubmit}>
          <div>
            <label className="text-sm text-gray-400 mb-1 block">
              {mode === 'login' ? 'Username or email' : 'Username'}
            </label>
            <Input
              value={form.username}
              onChange={(e) => updateField('username', e.target.value)}
              placeholder={mode === 'login' ? 'email@oasis.com' : 'Choose a handle'}
              className="bg-gray-900 border-gray-800 text-white placeholder:text-gray-600"
              disabled={isAuthenticating}
            />
          </div>

          {mode === 'register' && (
            <>
              <div>
                <label className="text-sm text-gray-400 mb-1 block">Email</label>
                <Input
                  type="email"
                  value={form.email}
                  onChange={(e) => updateField('email', e.target.value)}
                  placeholder="you@example.com"
                  className="bg-gray-900 border-gray-800 text-white placeholder:text-gray-600"
                  disabled={isAuthenticating}
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="text-sm text-gray-400 mb-1 block">First name</label>
                  <Input
                    value={form.firstName}
                    onChange={(e) => updateField('firstName', e.target.value)}
                    placeholder="Optional"
                    className="bg-gray-900 border-gray-800 text-white placeholder:text-gray-600"
                    disabled={isAuthenticating}
                  />
                </div>
                <div>
                  <label className="text-sm text-gray-400 mb-1 block">Last name</label>
                  <Input
                    value={form.lastName}
                    onChange={(e) => updateField('lastName', e.target.value)}
                    placeholder="Optional"
                    className="bg-gray-900 border-gray-800 text-white placeholder:text-gray-600"
                    disabled={isAuthenticating}
                  />
                </div>
              </div>
            </>
          )}

          <div>
            <label className="text-sm text-gray-400 mb-1 block">Password</label>
            <Input
              type="password"
              value={form.password}
              onChange={(e) => updateField('password', e.target.value)}
              placeholder="••••••••"
              className="bg-gray-900 border-gray-800 text-white placeholder:text-gray-600"
              disabled={isAuthenticating}
            />
          </div>

          {(localError || authError) && (
            <div className="text-sm text-red-400 bg-red-500/10 border border-red-500/30 rounded-lg px-3 py-2">
              {localError || authError}
            </div>
          )}

          <div className="space-y-3">
            <Button
              type="submit"
              className="w-full bg-white text-black hover:bg-white/90 text-base font-semibold"
              disabled={isAuthenticating}
            >
              {isAuthenticating ? (
                <>
                  <Loader2 className="w-4 h-4 mr-2 animate-spin" />
                  Connecting...
                </>
              ) : mode === 'login' ? (
                'Sign in'
              ) : (
                'Create avatar'
              )}
            </Button>
            <Button
              type="button"
              variant="outline"
              className="w-full border-white/20 text-white hover:bg-white/10"
              onClick={useDemoAvatar}
              disabled={isAuthenticating}
            >
              Skip for now
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
};

