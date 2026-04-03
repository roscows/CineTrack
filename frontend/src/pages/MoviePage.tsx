import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import type { Movie, Review } from '../lib/types';

type MoviePageProps = {
  movie: Movie | null;
  reviews: Review[];
  loading: boolean;
  reviewError: string;
  isInWatchlist: boolean;
  addingToWatchlist: boolean;
  onSubmitReview: (rating: number, content: string) => Promise<void>;
  onAddToWatchlist: () => Promise<void>;
};

export function MoviePage({
  movie,
  reviews,
  loading,
  reviewError,
  isInWatchlist,
  addingToWatchlist,
  onSubmitReview,
  onAddToWatchlist,
}: MoviePageProps) {
  const [rating, setRating] = useState(8);
  const [content, setContent] = useState('');
  const [sending, setSending] = useState(false);

  const dist = useMemo(() => {
    const total = reviews.length || 1;
    const top = reviews.filter((r) => r.rating === 10).length;
    const mid = reviews.filter((r) => r.rating >= 7 && r.rating <= 9).length;
    const low = reviews.filter((r) => r.rating <= 6).length;

    return {
      top: Math.round((top / total) * 100),
      mid: Math.round((mid / total) * 100),
      low: Math.round((low / total) * 100),
    };
  }, [reviews]);

  const averageRating = useMemo(() => {
    if (!movie) return 0;
    if (reviews.length === 0) return movie.averageRating;
    const sum = reviews.reduce((acc, review) => acc + review.rating, 0);
    return sum / reviews.length;
  }, [movie, reviews]);

  const handleSubmit = async () => {
    if (!content.trim()) return;
    setSending(true);
    try {
      await onSubmitReview(rating, content.trim());
      setContent('');
    } finally {
      setSending(false);
    }
  };

  if (loading) {
    return <section className="panel"><p className="muted">Ucitavanje detalja filma...</p></section>;
  }

  if (!movie) {
    return <section className="panel"><p className="error-text">Film nije pronadjen.</p></section>;
  }

  return (
    <section className="panel">
      <div className="movie-head">
        <Link to="/" className="back-link">Nazad</Link>
        <button className="pill-btn" onClick={onAddToWatchlist} disabled={isInWatchlist || addingToWatchlist}>
          {isInWatchlist ? 'U wishlisti' : addingToWatchlist ? 'Dodavanje...' : '+ Dodaj na listu'}
        </button>
      </div>

      <div className="movie-hero">
        <div className="hero-poster">
          {movie.posterUrl ? (
            <img className="hero-poster-image" src={movie.posterUrl} alt={`${movie.title} poster`} />
          ) : (
            <span>MOVIE</span>
          )}
        </div>
        <div className="hero-info">
          <h1>{movie.title}</h1>
          <p>{movie.year} - {movie.director}</p>
          <div className="tags-row">
            {movie.genres.map((g) => (
              <span key={g} className="tag">{g}</span>
            ))}
          </div>
          <div className="rating-summary">
            <div className="avg">{averageRating.toFixed(1)}</div>
            <div className="bars">
              <div className="bar-row"><span>10</span><div><i style={{ width: `${dist.top}%` }} /></div></div>
              <div className="bar-row"><span>7-9</span><div><i style={{ width: `${dist.mid}%` }} /></div></div>
              <div className="bar-row"><span>1-6</span><div><i style={{ width: `${dist.low}%` }} /></div></div>
            </div>
          </div>
        </div>
      </div>

      <div className="review-box">
        <h3>Tvoja ocena</h3>
        <div className="star-row">
          {Array.from({ length: 10 }, (_, idx) => {
            const value = idx + 1;
            return (
              <button key={value} className={`star ${value <= rating ? 'on' : ''}`} onClick={() => setRating(value)}>
                *
              </button>
            );
          })}
        </div>
        <textarea
          className="review-input"
          placeholder="Napisi recenziju..."
          value={content}
          onChange={(e) => setContent(e.target.value)}
        />
        <button className="pill-btn" onClick={handleSubmit} disabled={sending || !content.trim()}>
          {sending ? 'Slanje...' : 'Objavi recenziju'}
        </button>
        {reviewError && <p className="error-text">{reviewError}</p>}
      </div>

      <h3 className="section-title">Recenzije ({reviews.length})</h3>
      <div className="review-list">
        {reviews.map((review) => {
          const avatar = review.userAvatarUrl ?? '';
          return (
            <article className="review-card" key={review.id}>
              <div className="review-top">
                {avatar ? (
                  <img src={avatar} alt={`${review.username} avatar`} className="user-dot user-dot-image" />
                ) : (
                  <div className="user-dot">{review.username.slice(0, 2).toUpperCase()}</div>
                )}
                <strong>{review.username}</strong>
                <span className="score">* {review.rating}/10</span>
              </div>
              <p>{review.content}</p>
              <span className="muted small">{review.comments.length} komentara</span>
            </article>
          );
        })}
      </div>
    </section>
  );
}
