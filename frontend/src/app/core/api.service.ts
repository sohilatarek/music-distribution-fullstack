import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../environments/environment';
import { Artist, Dsp, TrackDetail, TrackFilters, TrackListItem } from './models';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly baseUrl = environment.apiBaseUrl;

  constructor(private http: HttpClient) {}

  getArtists(): Observable<Artist[]> {
    return this.http.get<Artist[]>(`${this.baseUrl}/artists`);
  }

  createArtist(data: { name: string; email: string; country: string }): Observable<Artist> {
    return this.http.post<Artist>(`${this.baseUrl}/artists`, data);
  }

  getDsps(): Observable<Dsp[]> {
    return this.http.get<Dsp[]>(`${this.baseUrl}/dsps`);
  }

  getTracks(filters: TrackFilters = {}): Observable<TrackListItem[]> {
    let params = new HttpParams();
    if (filters.artistId) params = params.set('artistId', filters.artistId);
    if (filters.genre) params = params.set('genre', filters.genre);
    if (filters.status) params = params.set('status', filters.status);

    return this.http.get<TrackListItem[]>(`${this.baseUrl}/tracks`, { params });
  }

  getTrack(id: number): Observable<TrackDetail> {
    return this.http.get<TrackDetail>(`${this.baseUrl}/tracks/${id}`);
  }

  createTrack(data: {
    title: string;
    artistId: number;
    isrc: string;
    releaseDate: string;
    genre: string;
  }): Observable<TrackListItem> {
    return this.http.post<TrackListItem>(`${this.baseUrl}/tracks`, data);
  }

  distributeTrack(id: number, dspIds: number[]): Observable<TrackDetail> {
    return this.http.post<TrackDetail>(`${this.baseUrl}/tracks/${id}/distribute`, { dspIds });
  }

  updateTrackStatus(id: number, status: string): Observable<TrackListItem> {
    return this.http.patch<TrackListItem>(`${this.baseUrl}/tracks/${id}/status`, { status });
  }
}
