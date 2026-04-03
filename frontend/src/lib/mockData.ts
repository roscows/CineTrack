import type { Movie, Review, UserProfile, WatchlistItem } from './types';

export const sampleMovies: Movie[] = [
  {
    id: 'm1',
    title: 'Oppenheimer',
    description: 'Biografska drama o razvoju atomske bombe.',
    year: 2023,
    genres: ['Drama', 'Istorija'],
    director: 'Christopher Nolan',
    cast: ['Cillian Murphy', 'Emily Blunt', 'Matt Damon'],
    posterUrl: '',
    averageRating: 8.7,
    reviewCount: 1200,
  },
  {
    id: 'm2',
    title: 'Dune: Part Two',
    description: 'Borba za Arrakis i sudbinu univerzuma.',
    year: 2024,
    genres: ['Sci-Fi', 'Akcija'],
    director: 'Denis Villeneuve',
    cast: ['Timothee Chalamet', 'Zendaya'],
    posterUrl: '',
    averageRating: 8.5,
    reviewCount: 830,
  },
  {
    id: 'm3',
    title: 'Poor Things',
    description: 'Fantasticna drama o identitetu i slobodi.',
    year: 2023,
    genres: ['Komedija', 'Drama'],
    director: 'Yorgos Lanthimos',
    cast: ['Emma Stone', 'Mark Ruffalo'],
    posterUrl: '',
    averageRating: 7.9,
    reviewCount: 450,
  },
];

export const sampleReviews: Review[] = [
  {
    id: 'r1',
    movieId: 'm1',
    userId: 'u1',
    username: 'ana_n',
    rating: 9,
    content: 'Neverovatna gluma i vizuali. Nolan je premašio sva ocekivanja.',
    comments: [
      {
        id: 'c1',
        userId: 'u2',
        username: 'marko_k',
        content: 'Slažem se, soundtrack je posebno jak.',
        createdAt: new Date().toISOString(),
      },
    ],
    createdAt: new Date(Date.now() - 2 * 24 * 60 * 60 * 1000).toISOString(),
    updatedAt: null,
  },
  {
    id: 'r2',
    movieId: 'm1',
    userId: 'u2',
    username: 'marko_k',
    rating: 8,
    content: 'Malo predugacak ali svaka scena ima svoju svrhu.',
    comments: [],
    createdAt: new Date(Date.now() - 5 * 24 * 60 * 60 * 1000).toISOString(),
    updatedAt: null,
  },
];

export const sampleWatchlist: WatchlistItem[] = [
  {
    id: 'w1',
    userId: 'u2',
    movieId: 'm1',
    movieTitle: 'Oppenheimer',
    moviePosterUrl: '',
    movieYear: 2023,
    watched: false,
    addedAt: new Date().toISOString(),
  },
  {
    id: 'w2',
    userId: 'u2',
    movieId: 'm2',
    movieTitle: 'Dune: Part Two',
    moviePosterUrl: '',
    movieYear: 2024,
    watched: true,
    addedAt: new Date().toISOString(),
    watchedAt: new Date().toISOString(),
  },
];

export const sampleProfile: UserProfile = {
  id: 'u2',
  username: 'marko_r',
  email: 'marko@example.com',
  avatarUrl: '',
  isAdmin: false,
  createdAt: '2024-01-10T00:00:00.000Z',
};

