import { useRef, useState } from 'react';
import type { Review, UserProfile } from '../lib/types';

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

type ProfilePageProps = {
  profile: UserProfile | null;
  reviews: Review[];
  watchedCount: number;
  loading: boolean;
  onLogout: () => void;
  onAvatarUpload: (avatarUrl: string) => Promise<void>;
};

export function ProfilePage({ profile, reviews, watchedCount, loading, onLogout, onAvatarUpload }: ProfilePageProps) {
  const fileInputRef = useRef<HTMLInputElement | null>(null);
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState('');

  if (loading) {
    return <section className="panel"><p className="muted">Ucitavanje profila...</p></section>;
  }

  if (!profile) {
    return (
      <section className="panel">
        <p className="muted">Profil nije dostupan. Prijavi se i osvezi stranicu.</p>
      </section>
    );
  }

  const avg = reviews.length ? (reviews.reduce((sum, r) => sum + r.rating, 0) / reviews.length).toFixed(1) : '0.0';

  const handleAvatarClick = () => {
    if (uploading) return;
    fileInputRef.current?.click();
  };

  const handleAvatarChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (file.size > MAX_AVATAR_SIZE_BYTES) {
      setUploadError('Slika je prevelika. Maksimalna velicina originala je 8MB.');
      e.target.value = '';
      return;
    }

    setUploading(true);
    setUploadError('');

    try {
      const processed = await processAvatar(file);
      await onAvatarUpload(processed);
    } catch {
      setUploadError('Neuspesno azuriranje avatara. Pokusaj ponovo.');
    } finally {
      setUploading(false);
      e.target.value = '';
    }
  };

  return (
    <section className="panel profile-panel">
      <button type="button" className="avatar-upload-btn" onClick={handleAvatarClick} disabled={uploading} title="Promeni avatar">
        {profile.avatarUrl ? (
          <img src={profile.avatarUrl} alt="Profil" className="avatar-large avatar-large-image" />
        ) : (
          <div className="avatar-large">{profile.username.slice(0, 2).toUpperCase()}</div>
        )}
      </button>
      <input ref={fileInputRef} type="file" accept="image/*" className="hidden-file-input" onChange={handleAvatarChange} />
      {uploading && <p className="muted">Upload avatara...</p>}
      {uploadError && <p className="error-text">{uploadError}</p>}

      <h2>{profile.username}</h2>
      <p className="muted">Clan od {new Date(profile.createdAt).toLocaleDateString('sr-RS')}</p>

      <div className="stats-row">
        <div><strong>{watchedCount}</strong><span>filmova</span></div>
        <div><strong>{reviews.length}</strong><span>recenzija</span></div>
        <div><strong>{avg}</strong><span>avg ocena</span></div>
      </div>

      <h3 className="section-title">Poslednje recenzije</h3>
      <div className="recent-list">
        {reviews.slice(0, 6).map((review) => (
          <div key={review.id} className="recent-row">
            <span>{review.content.slice(0, 40)}...</span>
            <strong>* {review.rating}</strong>
          </div>
        ))}
      </div>

      <button className="logout-bottom" onClick={onLogout}>Logout</button>
    </section>
  );
}
