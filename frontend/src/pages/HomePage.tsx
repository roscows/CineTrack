import { useMemo } from 'react';
import { Link } from 'react-router-dom';
import type { Movie } from '../lib/types';

type HomePageProps = {
  movies: Movie[];
  loading: boolean;
  search: string;
  genre: string;
  genres: string[];
  error: string;
  onSearchChange: (value: string) => void;
  onGenreChange: (genre: string) => void;
};

export function HomePage({ movies, loading, search, genre, genres, error, onSearchChange, onGenreChange }: HomePageProps) {
  const title = useMemo(() => (search.trim() ? `Rezultati za "${search}"` : 'Popularno ove nedelje'), [search]);

  return (
    <section className="panel">
      <div className="search-wrap">
        <input
          className="search-input"
          placeholder="Pretrazi filmove po imenu..."
          value={search}
          onChange={(e) => onSearchChange(e.target.value)}
        />
      </div>

      <div className="filters-row">
        <button className={`chip ${genre === 'Sve' ? 'active' : ''}`} onClick={() => onGenreChange('Sve')}>
          Sve
        </button>
        {genres.map((tag) => (
          <button key={tag} className={`chip ${genre === tag ? 'active' : ''}`} onClick={() => onGenreChange(tag)}>
            {tag}
          </button>
        ))}
      </div>

      <h2 className="section-title">{title}</h2>

      {loading && <p className="muted">Ucitavanje filmova...</p>}
      {!loading && error && <p className="error-text">{error}</p>}
      {!loading && !error && movies.length === 0 && <p className="muted">Nema filmova za izabrane filtere.</p>}

      {!loading && !error && movies.length > 0 && (
        <div className="movie-grid">
          {movies.map((movie) => (
            <Link key={movie.id} to={`/movies/${movie.id}`} className="movie-card">
              <div className="poster-gradient">
                {movie.posterUrl ? (
                  <img className="poster-image" src={movie.posterUrl} alt={`${movie.title} poster`} loading="lazy" />
                ) : (
                  <span className="poster-icon">MOV</span>
                )}
              </div>
              <div className="movie-body">
                <h3>{movie.title}</h3>
                <p className="year">{movie.year}</p>
                <div className="rating-row">* {movie.averageRating.toFixed(1)}</div>
                <div className="tags-row">
                  {movie.genres.slice(0, 3).map((g) => (
                    <span className="tag" key={`${movie.id}-${g}`}>
                      {g}
                    </span>
                  ))}
                </div>
              </div>
            </Link>
          ))}
        </div>
      )}
    </section>
  );
}
