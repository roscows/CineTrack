import type { Review } from '../lib/types';

type ReviewsPageProps = {
  reviews: Review[];
  loading: boolean;
};

export function ReviewsPage({ reviews, loading }: ReviewsPageProps) {
  return (
    <section className="panel">
      <h2 className="section-title">Sve napisane recenzije</h2>

      {loading && <p className="muted">Ucitavanje recenzija...</p>}
      {!loading && reviews.length === 0 && <p className="muted">Nema dostupnih recenzija za trenutnog korisnika.</p>}

      {!loading && reviews.length > 0 && (
        <div className="review-list">
          {reviews.map((review) => (
            <article className="review-card" key={review.id}>
              <div className="review-top">
                <div className="user-dot">{review.username.slice(0, 2).toUpperCase()}</div>
                <strong>{review.username}</strong>
                <span className="score">* {review.rating}/10</span>
              </div>
              <p>{review.content}</p>
              <span className="muted small">Komentara: {review.comments.length}</span>
            </article>
          ))}
        </div>
      )}
    </section>
  );
}
