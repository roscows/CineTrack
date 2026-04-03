import type {
  AuthResponse,
  LoginRequest,
  Movie,
  RegisterRequest,
  Review,
  UserProfile,
  WatchlistItem,
} from './types';

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5265/api';

const AUTH_KEYS = ['mt_token', 'mt_user_id', 'mt_username', 'mt_avatar_url', 'mt_is_admin'] as const;

export function clearStoredAuth() {
  AUTH_KEYS.forEach((key) => {
    sessionStorage.removeItem(key);
    localStorage.removeItem(key);
  });
}

function getToken() {
  return sessionStorage.getItem('mt_token');
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = getToken();
  const headers = new Headers(init?.headers);
  headers.set('Content-Type', 'application/json');

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  let response: Response;
  try {
    response = await fetch(`${API_BASE}${path}`, {
      ...init,
      headers,
    });
  } catch {
    throw new Error(`Network error. API nije dostupan na ${API_BASE}. Proveri da li backend radi.`);
  }

  if (!response.ok) {
    if (response.status === 401) {
      clearStoredAuth();
      window.dispatchEvent(new CustomEvent('mt:unauthorized'));
    }

    const message = await response.text();
    throw new Error(message || `Request failed: ${response.status}`);
  }

  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}

export function loginUser(payload: LoginRequest): Promise<AuthResponse> {
  return request<AuthResponse>('/users/login', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

export function registerUser(payload: RegisterRequest): Promise<void> {
  return request<void>('/users/register', {
    method: 'POST',
    body: JSON.stringify(payload),
  });
}

export function updateMyAvatar(avatarUrl: string): Promise<UserProfile> {
  return request<UserProfile>('/users/me/avatar', {
    method: 'PATCH',
    body: JSON.stringify({ avatarUrl }),
  });
}

export async function fetchMovies(search = '', genre = ''): Promise<Movie[]> {
  const query = new URLSearchParams();
  if (search.trim()) query.set('search', search.trim());
  if (genre.trim() && genre !== 'Sve') query.set('genre', genre.trim());
  query.set('page', '1');
  query.set('pageSize', '100');

  const data = await request<{ items: Movie[] }>(`/movies?${query.toString()}`);
  return data.items;
}

export function fetchMovie(id: string): Promise<Movie> {
  return request<Movie>(`/movies/${id}`);
}

export function fetchMovieReviews(movieId: string): Promise<Review[]> {
  return request<Review[]>(`/movies/${movieId}/reviews`);
}

export function createReview(movieId: string, rating: number, content: string): Promise<Review> {
  return request<Review>('/reviews', {
    method: 'POST',
    body: JSON.stringify({ movieId, rating, content }),
  });
}

export function addToWatchlist(movieId: string): Promise<WatchlistItem> {
  return request<WatchlistItem>('/watchlist', {
    method: 'POST',
    body: JSON.stringify({ movieId }),
  });
}

export function fetchWatchlist(): Promise<WatchlistItem[]> {
  return request<WatchlistItem[]>('/watchlist');
}

export function markWatchlistWatched(id: string): Promise<void> {
  return request<void>(`/watchlist/${id}/watched`, { method: 'PATCH' });
}

export function fetchUserProfile(id: string): Promise<UserProfile> {
  return request<UserProfile>(`/users/${id}/profile`);
}

export function fetchUserReviews(id: string): Promise<Review[]> {
  return request<Review[]>(`/users/${id}/reviews`);
}
