import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { registerUser } from '../lib/api';

const MAX_AVATAR_SIZE_BYTES = 8 * 1024 * 1024;
const AVATAR_SIZE = 256;

function loadImageFromFile(file: File): Promise<HTMLImageElement> {
  return new Promise((resolve, reject) => {
    const objectUrl = URL.createObjectURL(file);
    const img = new Image();

    img.onload = () => {
      URL.revokeObjectURL(objectUrl);
      resolve(img);
    };

    img.onerror = () => {
      URL.revokeObjectURL(objectUrl);
      reject(new Error('Neuspesno ucitavanje slike.'));
    };

    img.src = objectUrl;
  });
}

async function processAvatar(file: File): Promise<string> {
  const img = await loadImageFromFile(file);

  const sourceSize = Math.min(img.naturalWidth, img.naturalHeight);
  const sx = Math.floor((img.naturalWidth - sourceSize) / 2);
  const sy = Math.floor((img.naturalHeight - sourceSize) / 2);

  const canvas = document.createElement('canvas');
  canvas.width = AVATAR_SIZE;
  canvas.height = AVATAR_SIZE;

  const ctx = canvas.getContext('2d');
  if (!ctx) {
    throw new Error('Neuspesna obrada slike.');
  }

  ctx.imageSmoothingEnabled = true;
  ctx.imageSmoothingQuality = 'high';
  ctx.drawImage(img, sx, sy, sourceSize, sourceSize, 0, 0, AVATAR_SIZE, AVATAR_SIZE);

  return canvas.toDataURL('image/jpeg', 0.9);
}

export function RegisterPage() {
  const navigate = useNavigate();
  const [username, setUsername] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [avatarUrl, setAvatarUrl] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const onAvatarChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) {
      setAvatarUrl('');
      return;
    }

    if (file.size > MAX_AVATAR_SIZE_BYTES) {
      setError('Slika je prevelika. Maksimalna velicina originala je 8MB.');
      e.target.value = '';
      return;
    }

    try {
      const processed = await processAvatar(file);
      setAvatarUrl(processed);
      setError('');
    } catch {
      setError('Neuspesno ucitavanje/obrada slike. Pokusaj ponovo.');
      e.target.value = '';
    }
  };

  const onSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');

    try {
      await registerUser({ username, email, password, avatarUrl });
      navigate('/login');
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Registracija nije uspela.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <section className="panel auth-panel">
      <h2>Register</h2>
      <form className="auth-form" onSubmit={onSubmit}>
        <label>Username</label>
        <input value={username} onChange={(e) => setUsername(e.target.value)} required />

        <label>Email</label>
        <input type="email" value={email} onChange={(e) => setEmail(e.target.value)} required />

        <label>Password</label>
        <input type="password" minLength={6} value={password} onChange={(e) => setPassword(e.target.value)} required />

        <label>Avatar (opciono)</label>
        <input type="file" accept="image/*" onChange={onAvatarChange} />

        {avatarUrl && (
          <div className="avatar-preview-wrap">
            <img src={avatarUrl} alt="Avatar preview" className="avatar-preview" />
          </div>
        )}

        {error && <p className="error-text">{error}</p>}

        <button className="pill-btn" disabled={loading}>{loading ? 'Kreiranje...' : 'Register'}</button>
      </form>
      <p className="muted">
        Vec imas nalog? <Link to="/login" className="inline-link">Login</Link>
      </p>
    </section>
  );
}
