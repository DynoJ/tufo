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
  private base = '/api'; // ← CHANGED

  constructor(private http: HttpClient) {}

  getClimbs(): Observable<Climb[]> {
    return this.http.get<Climb[]>(`${this.base}/climbs`);
  }

  getClimbById(id: number): Observable<Climb> {
    return this.http.get<Climb>(`${this.base}/climbs/${id}`);
  }

  addNote(id: number, body: string): Observable<RouteNote> {
    const url = `${this.base}/climbs/${id}/notes`;
    console.log('API call URL:', url);
    console.log('ID:', id, 'Body:', body);
    return this.http.post<RouteNote>(url, { body });
  }

  uploadMedia(id: number, file: File, caption?: string): Observable<Media> {
    const form = new FormData();
    form.append('file', file);
    if (caption) form.append('caption', caption);
    return this.http.post<Media>(`${this.base}/climbs/${id}/media`, form);
  }
}