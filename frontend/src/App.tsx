import { useCallback, useEffect, useMemo, useState } from 'react';
import { Link, NavLink, Route, Routes, useParams } from 'react-router-dom';
import './App.css';
import {
  addToWatchlist,
  clearStoredAuth,
  createReview,
  fetchMovie,
  fetchMovieReviews,
  fetchMovies,
  fetchUserProfile,
  fetchUserReviews,
  fetchWatchlist,
  markWatchlistWatched,
  updateMyAvatar,
} from './lib/api';
import type { Movie, Review, UserProfile, WatchlistItem } from './lib/types';
import { HomePage } from './pages/HomePage';
import { LoginPage } from './pages/LoginPage';
import { MoviePage } from './pages/MoviePage';
import { ProfilePage } from './pages/ProfilePage';
import { RegisterPage } from './pages/RegisterPage';
import { ReviewsPage } from './pages/ReviewsPage';
import { WatchlistPage } from './pages/WatchlistPage';

type AuthState = {
  token: string;
  userId: string;
  username: string;
  avatarUrl: string;
};

function getInitials(name: string) {
  const clean = name.trim();
  if (!clean) return 'MR';

  const parts = clean.split(/[\s_-]+/).filter(Boolean);
  if (parts.length >= 2) {
    return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
  }

  if (parts[0].length >= 2) {
    return `${parts[0][0]}${parts[0][1]}`.toUpperCase();
  }

  return `${parts[0][0]}${parts[0][0]}`.toUpperCase();
}

function App() {
  const [auth, setAuth] = useState<AuthState | null>(() => {
    const token = sessionStorage.getItem('mt_token') ?? '';
    const userId = sessionStorage.getItem('mt_user_id') ?? '';
    const username = sessionStorage.getItem('mt_username') ?? '';
    const avatarUrl = sessionStorage.getItem('mt_avatar_url') ?? '';

    return token && userId ? { token, userId, username, avatarUrl } : null;
  });

  useEffect(() => {
    if (!auth?.userId) return;

    let active = true;

    const load = async () => {
      try {
        const profile = await fetchUserProfile(auth.userId);
        if (!active) return;

        sessionStorage.setItem('mt_username', profile.username);
        sessionStorage.setItem('mt_avatar_url', profile.avatarUrl ?? '');

        setAuth((prev) => (prev ? { ...prev, username: profile.username, avatarUrl: profile.avatarUrl ?? '' } : prev));
      } catch {
        // keep cached values
      }
    };

    void load();

    return () => {
      active = false;
    };
  }, [auth?.userId]);

  const handleLoginSuccess = async (token: string, userId: string, username: string) => {
    sessionStorage.setItem('mt_token', token);
    sessionStorage.setItem('mt_user_id', userId);
    sessionStorage.setItem('mt_username', username);

    let avatarUrl = '';
    try {
      const profile = await fetchUserProfile(userId);
      avatarUrl = profile.avatarUrl ?? '';
      sessionStorage.setItem('mt_avatar_url', avatarUrl);
    } catch {
      sessionStorage.setItem('mt_avatar_url', '');
    }

    setAuth({ token, userId, username, avatarUrl });
  };

  const handleAvatarUpdated = async (avatarUrl: string) => {
    const updated = await updateMyAvatar(avatarUrl);
    sessionStorage.setItem('mt_avatar_url', updated.avatarUrl ?? '');
    sessionStorage.setItem('mt_username', updated.username ?? '');

    setAuth((prev) => (prev ? { ...prev, username: updated.username, avatarUrl: updated.avatarUrl ?? '' } : prev));
  };

  const clearAuth = useCallback(() => {
    clearStoredAuth();
    setAuth(null);
  }, []);

  useEffect(() => {
    const handleUnauthorized = () => {
      setAuth(null);
    };

    window.addEventListener('mt:unauthorized', handleUnauthorized);
    return () => window.removeEventListener('mt:unauthorized', handleUnauthorized);
  }, []);

  const avatarInitials = useMemo(() => getInitials(auth?.username ?? 'MR'), [auth?.username]);

  return (
    <div className="app-shell">
      <header className="topbar">
        <Link to="/" className="brand">CineTrack</Link>

        <nav className="main-nav">
          <NavLink to="/" end>Filmovi</NavLink>
          <NavLink to="/wishlist">Wishlist</NavLink>
          <NavLink to="/reviews">Recenzije</NavLink>
        </nav>

        <div className="top-actions">
          {!auth ? (
            <>
              <Link to="/login" className="top-btn">Login</Link>
              <Link to="/register" className="top-btn">Register</Link>
            </>
          ) : (
            <Link to="/profile" className="avatar-link" title="Profil">
              {auth.avatarUrl ? (
                <img src={auth.avatarUrl} alt="Profil" className="avatar avatar-image" />
              ) : (
                <div className="avatar">{avatarInitials}</div>
              )}
            </Link>
          )}
        </div>
      </header>

      <main className="content-wrap">
        <Routes>
          <Route path="/" element={<HomeRoute />} />
          <Route path="/movies/:id" element={<MovieRoute auth={auth} />} />
          <Route path="/wishlist" element={<WatchlistRoute auth={auth} />} />
          <Route path="/reviews" element={<ReviewsRoute auth={auth} />} />
          <Route path="/profile" element={<ProfileRoute auth={auth} onLogout={clearAuth} onAvatarUpload={handleAvatarUpdated} />} />
          <Route path="/login" element={<LoginPage onLoginSuccess={handleLoginSuccess} />} />
          <Route path="/register" element={<RegisterPage />} />
        </Routes>
      </main>
    </div>
  );
}

function HomeRoute() {
  const [allMovies, setAllMovies] = useState<Movie[]>([]);
  const [search, setSearch] = useState('');
  const [genre, setGenre] = useState('Sve');
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');

  useEffect(() => {
    let active = true;

    const run = async () => {
      setLoading(true);
      setError('');
      try {
        const data = await fetchMovies();
        if (!active) return;
        setAllMovies(data);
      } catch {
        if (!active) return;
        setError('Filmovi trenutno nisu dostupni. Proveri backend API.');
        setAllMovies([]);
      } finally {
        if (active) setLoading(false);
      }
    };

    void run();

    return () => {
      active = false;
    };
  }, []);

  const genres = useMemo(() => {
    const set = new Set<string>();
    allMovies.forEach((m) => m.genres.forEach((g) => set.add(g)));
    return Array.from(set).sort((a, b) => a.localeCompare(b));
  }, [allMovies]);

  const filteredMovies = useMemo(() => {
    const query = search.trim().toLowerCase();

    return allMovies.filter((movie) => {
      const genreMatch = genre === 'Sve' || movie.genres.some((g) => g.toLowerCase() === genre.toLowerCase());
      const titleMatch = !query || movie.title.toLowerCase().includes(query);
      return genreMatch && titleMatch;
    });
  }, [allMovies, search, genre]);

  return (
    <HomePage
      movies={filteredMovies}
      loading={loading}
      search={search}
      genre={genre}
      genres={genres}
      error={error}
      onSearchChange={setSearch}
      onGenreChange={setGenre}
    />
  );
}

function MovieRoute({ auth }: { auth: AuthState | null }) {
  const { id } = useParams();

  const [movie, setMovie] = useState<Movie | null>(null);
  const [reviews, setReviews] = useState<Review[]>([]);
  const [loading, setLoading] = useState(true);
  const [reviewError, setReviewError] = useState('');
  const [isInWatchlist, setIsInWatchlist] = useState(false);
  const [addingToWatchlist, setAddingToWatchlist] = useState(false);

  const load = async (movieId: string) => {
    setLoading(true);
    try {
      const [m, r] = await Promise.all([fetchMovie(movieId), fetchMovieReviews(movieId)]);
      setMovie(m);
      setReviews(r);
      setReviewError('');

      if (auth?.userId) {
        try {
          const watchlist = await fetchWatchlist();
          setIsInWatchlist(watchlist.some((item) => item.movieId === movieId));
        } catch {
          setIsInWatchlist(false);
        }
      } else {
        setIsInWatchlist(false);
      }
    } catch {
      setMovie(null);
      setReviews([]);
      setReviewError('Detalji filma nisu dostupni.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!id) return;
    void load(id);
  }, [id, auth?.userId]);

  const handleReview = async (rating: number, content: string) => {
    if (!movie) return;

    try {
      setReviewError('');
      const created = await createReview(movie.id, rating, content);
      setReviews((prev) => [created, ...prev]);

      try {
        const updatedMovie = await fetchMovie(movie.id);
        setMovie(updatedMovie);
      } catch {
        // keep UI state
      }
    } catch (e) {
      setReviewError(e instanceof Error ? e.message : 'Neuspesno slanje recenzije.');
    }
  };

  const handleAddToWatchlist = async () => {
    if (!movie) return;
    if (!auth) {
      setReviewError('Dodavanje na wishlist nije uspelo. Potrebna je prijava.');
      return;
    }

    if (isInWatchlist) {
      return;
    }

    try {
      setAddingToWatchlist(true);
      await addToWatchlist(movie.id);
      setIsInWatchlist(true);
    } catch (e) {
      const message = e instanceof Error ? e.message : '';
      if (message.toLowerCase().includes('duplicate') || message.includes('vec')) {
        setIsInWatchlist(true);
        return;
      }

      setReviewError('Dodavanje na wishlist nije uspelo.');
    } finally {
      setAddingToWatchlist(false);
    }
  };

  return (
    <MoviePage
      movie={movie}
      reviews={reviews}
      loading={loading}
      reviewError={reviewError}
      isInWatchlist={isInWatchlist}
      addingToWatchlist={addingToWatchlist}
      onSubmitReview={handleReview}
      onAddToWatchlist={handleAddToWatchlist}
    />
  );
}

function WatchlistRoute({ auth }: { auth: AuthState | null }) {
  const [items, setItems] = useState<WatchlistItem[]>([]);
  const [loading, setLoading] = useState(true);
  const [filter, setFilter] = useState<'all' | 'pending'>('all');
  const [error, setError] = useState('');

  const load = async () => {
    setLoading(true);
    setError('');

    try {
      const data = await fetchWatchlist();
      setItems(data);
    } catch {
      setItems([]);
      setError('Wishlist nije dostupna. Proveri da li si prijavljen.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    void load();
  }, []);

  const handleToggle = async (itemId: string, watched: boolean) => {
    if (watched) return;

    try {
      await markWatchlistWatched(itemId);
      setItems((prev) => prev.map((x) => (x.id === itemId ? { ...x, watched: true } : x)));
    } catch {
      setError('Oznacavanje kao pogledano nije uspelo.');
    }
  };

  if (!auth) {
    return <AuthRequiredNotice message="Da bi video wishlist, potrebno je da se prijavis." />;
  }

  return (
    <>
      <WatchlistPage
        items={items}
        loading={loading}
        filter={filter}
        onFilterChange={setFilter}
        onToggleWatched={handleToggle}
      />
      {error && <p className="error-text top-error">{error}</p>}
    </>
  );
}

function ProfileRoute({
  auth,
  onLogout,
  onAvatarUpload,
}: {
  auth: AuthState | null;
  onLogout: () => void;
  onAvatarUpload: (avatarUrl: string) => Promise<void>;
}) {
  const [profile, setProfile] = useState<UserProfile | null>(null);
  const [reviews, setReviews] = useState<Review[]>([]);
  const [loading, setLoading] = useState(true);
  const [watchedCount, setWatchedCount] = useState(0);

  useEffect(() => {
    if (!auth?.userId) {
      setProfile(null);
      setReviews([]);
      setWatchedCount(0);
      setLoading(false);
      return;
    }

    let active = true;

    const run = async () => {
      setLoading(true);
      try {
        const [user, userReviews, watchlist] = await Promise.all([
          fetchUserProfile(auth.userId),
          fetchUserReviews(auth.userId),
          fetchWatchlist(),
        ]);

        if (!active) return;

        setProfile(user);
        setReviews(userReviews);
        setWatchedCount(watchlist.filter((x) => x.watched).length);
      } catch {
        if (!active) return;
        setProfile(null);
        setReviews([]);
        setWatchedCount(0);
      } finally {
        if (active) setLoading(false);
      }
    };

    void run();

    return () => {
      active = false;
    };
  }, [auth?.userId]);

  const handleAvatarUpload = async (avatarUrl: string) => {
    await onAvatarUpload(avatarUrl);

    if (!auth?.userId) return;
    const refreshed = await fetchUserProfile(auth.userId);
    setProfile(refreshed);
  };

  if (!auth) {
    return <AuthRequiredNotice message="Profil je dostupan nakon prijave." />;
  }

  return (
    <ProfilePage
      profile={profile}
      reviews={reviews}
      watchedCount={watchedCount}
      loading={loading}
      onLogout={onLogout}
      onAvatarUpload={handleAvatarUpload}
    />
  );
}

function ReviewsRoute({ auth }: { auth: AuthState | null }) {
  const [reviews, setReviews] = useState<Review[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!auth?.userId) {
      setReviews([]);
      setLoading(false);
      return;
    }

    let active = true;

    const run = async () => {
      setLoading(true);
      try {
        const data = await fetchUserReviews(auth.userId);
        if (!active) return;
        setReviews(data);
      } catch {
        if (!active) return;
        setReviews([]);
      } finally {
        if (active) setLoading(false);
      }
    };

    void run();

    return () => {
      active = false;
    };
  }, [auth?.userId]);

  if (!auth) {
    return <AuthRequiredNotice message="Recenzije su dostupne nakon prijave." />;
  }

  return <ReviewsPage reviews={reviews} loading={loading} />;
}

function AuthRequiredNotice({ message }: { message: string }) {
  return (
    <section className="panel">
      <p className="muted">{message}</p>
      <div className="auth-links-inline">
        <Link to="/login" className="inline-link">Login</Link>
        <Link to="/register" className="inline-link">Register</Link>
      </div>
    </section>
  );
}

export default App;
