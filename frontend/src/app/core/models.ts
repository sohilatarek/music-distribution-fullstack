export type TrackStatus = 'draft' | 'submitted' | 'distributed';
export type DistributionStatus = 'pending' | 'live' | 'rejected';

export interface Artist {
  id: number;
  name: string;
  email: string;
  country: string;
}

export interface Dsp {
  id: number;
  name: string;
}

export interface TrackListItem {
  id: number;
  title: string;
  artistId: number;
  artistName: string;
  isrc: string;
  releaseDate: string;
  genre: string;
  status: TrackStatus;
}

export interface TrackDistributionItem {
  id: number;
  dspId: number;
  dspName: string;
  submittedAt: string;
  status: DistributionStatus;
}

export interface TrackDetail extends TrackListItem {
  distributions: TrackDistributionItem[];
}

export interface TrackFilters {
  artistId?: number;
  genre?: string;
  status?: string;
}

export interface LoginResponse {
  token: string;
  expiresAt: string;
}
