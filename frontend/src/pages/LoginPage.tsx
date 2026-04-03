import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { loginUser } from '../lib/api';

type LoginPageProps = {
  onLoginSuccess: (token: string, userId: string, username: string) => Promise<void>;
};

export function LoginPage({ onLoginSuccess }: LoginPageProps) {
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      const auth = await loginUser({ email, password });
      await onLoginSuccess(auth.token, auth.userId, auth.username);
      navigate('/');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Login nije uspeo.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className="panel auth-panel">
      <h2>Login</h2>
      <form className="auth-form" onSubmit={onSubmit}>
        <label>Email</label>
        <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />

        <label>Password</label>
        <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />

        {error && <p className="error-text">{error}</p>}

        <button className="pill-btn" disabled={loading}>{loading ? 'Prijava...' : 'Login'}</button>
      </form>
      <p className="muted">
        Nemas nalog? <Link to="/register" className="inline-link">Register</Link>
      </p>
    </section>
  );
}
