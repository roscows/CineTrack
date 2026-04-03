import type { WatchlistItem } from '../lib/types';

type WatchlistPageProps = {
  items: WatchlistItem[];
  loading: boolean;
  filter: 'all' | 'pending';
  onFilterChange: (filter: 'all' | 'pending') => void;
  onToggleWatched: (id: string, watched: boolean) => Promise<void>;
};

export function WatchlistPage({ items, loading, filter, onFilterChange, onToggleWatched }: WatchlistPageProps) {
  const filtered = filter === 'pending' ? items.filter((x) => !x.watched) : items;
  const watchedCount = items.filter((x) => x.watched).length;

  return (
    <section className="panel">
      <div className="list-head">
        <h2>Wishlist</h2>
        <div className="filters-row compact">
          <button className={`chip ${filter === 'all' ? 'active' : ''}`} onClick={() => onFilterChange('all')}>Sve</button>
          <button className={`chip ${filter === 'pending' ? 'active' : ''}`} onClick={() => onFilterChange('pending')}>Za gledanje</button>
        </div>
      </div>

      {loading ? (
        <p className="muted">Ucitavanje liste...</p>
      ) : (
        <div className="watchlist-grid">
          {filtered.map((item) => (
            <article key={item.id} className="watch-item">
              <div className="watch-left">
                <div className="tiny-poster">
                  {item.moviePosterUrl ? (
                    <img className="tiny-poster-image" src={item.moviePosterUrl} alt={`${item.movieTitle} poster`} loading="lazy" />
                  ) : (
                    'FILM'
                  )}
                </div>
                <div>
                  <h4>{item.movieTitle}</h4>
                  <p>{item.movieYear}</p>
                  {item.watched && <span className="watched-badge">Pogledano</span>}
                </div>
              </div>
              <label className="check-wrap">
                <input
                  type="checkbox"
                  checked={item.watched}
                  onChange={() => onToggleWatched(item.id, item.watched)}
                  disabled={item.watched}
                />
              </label>
            </article>
          ))}
        </div>
      )}

      <footer className="panel-footer">
        <span>{items.length} filmova na listi</span>
        <span>{watchedCount} pogledano</span>
      </footer>
    </section>
  );
}
