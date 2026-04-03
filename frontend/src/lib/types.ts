export type Movie = {
  id: string;
  title: string;
  description: string;
  year: number;
  genres: string[];
  director: string;
  cast: string[];
  posterUrl: string;
  averageRating: number;
  reviewCount: number;
};

export type Comment = {
  id: string;
  userId: string;
  username: string;
  content: string;
  createdAt: string;
};

export type Review = {
  id: string;
  movieId: string;
  userId: string;
  username: string;
  userAvatarUrl?: string;
  rating: number;
  content: string;
  comments: Comment[];
  createdAt: string;
  updatedAt?: string | null;
};

export type WatchlistItem = {
  id: string;
  userId: string;
  movieId: string;
  movieTitle: string;
  moviePosterUrl: string;
  movieYear: number;
  watched: boolean;
  addedAt: string;
  watchedAt?: string | null;
};

export type UserProfile = {
  id: string;
  username: string;
  email: string;
  avatarUrl: string;
  isAdmin: boolean;
  createdAt: string;
};

export type AuthResponse = {
  token: string;
  userId: string;
  username: string;
  email: string;
  isAdmin: boolean;
};

export type LoginRequest = {
  email: string;
  password: string;
};

export type RegisterRequest = {
  username: string;
  email: string;
  password: string;
  avatarUrl: string;
};
