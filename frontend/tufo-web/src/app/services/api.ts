import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Area {
  id: number;
  name: string;
  state: string;
  country: string;
  lat?: number;
  lng?: number;
}

export interface Media {
  id: number;
  climbId: number;
  type: number; // 0=photo, 1=video
  url: string;
  thumbnailUrl?: string;
  caption?: string;
  createdAt?: string;
}

export interface RouteNote {
  id: number;
  body: string;
  createdAt: string;
}

export interface Climb {
  id: number;
  name: string;
  yds: string;
  type: string;
  description?: string;
  heroUrl?: string;
  heroAttribution?: string;
  area?: Area;
  media: Media[];
  notes: RouteNote[];
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private base = 'http://localhost:5038/api';

  constructor(private http: HttpClient) {}

  getClimbs(): Observable<Climb[]> {
    return this.http.get<Climb[]>(`${this.base}/climbs`);
  }

  getClimbById(id: number): Observable<Climb> {
    return this.http.get<Climb>(`${this.base}/climbs/${id}`);
  }

  addNote(id: number, body: string): Observable<RouteNote> {
    return this.http.post<RouteNote>(`${this.base}/climbs/${id}/notes`, { body });
  }

  uploadMedia(id: number, file: File, caption?: string): Observable<Media> {
    const form = new FormData();
    form.append('file', file);
    if (caption) form.append('caption', caption);
    return this.http.post<Media>(`${this.base}/climbs/${id}/media`, form);
  }
}