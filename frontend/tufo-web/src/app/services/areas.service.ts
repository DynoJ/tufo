import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

export interface Area {
  id: number;
  name: string;
  state?: string;
  lat?: number;
  lng?: number;
  parentAreaId?: number;
  subAreas: SubArea[];
  climbs: ClimbSummary[];
}

export interface SubArea {
  id: number;
  name: string;
  climbCount: number;
}

export interface ClimbSummary {
  id: number;
  name: string;
  type: string;
  yds?: string;
}

export interface SearchImportRequest {
  areaName: string;
}

export interface ImportResult {
  success: boolean;
  message: string;
  areasImported: number;
  climbsImported: number;
  climbsSkipped: number;
  errors: string[];
}

export interface DeleteResult {
  success: boolean;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class AreasService {
  private apiUrl = 'http://localhost:5038/api';

  constructor(private http: HttpClient) {}

  /**
   * Get all top-level areas (no parent) with climb counts
   */
  getTopLevelAreas(): Observable<SubArea[]> {
    return this.http.get<SubArea[]>(`${this.apiUrl}/Areas`);
  }

  /**
   * Get area details including sub-areas and climbs
   */
  getAreaDetails(id: number): Observable<Area> {
    return this.http.get<Area>(`${this.apiUrl}/Areas/${id}`);
  }

  /**
   * Search for areas by name
   */
  searchAreas(query: string): Observable<Area[]> {
    return this.http.get<Area[]>(`${this.apiUrl}/Areas/search?q=${encodeURIComponent(query)}`);
  }

  /**
   * Import a location by name (e.g., "Barton Creek Greenbelt")
   */
  importLocation(areaName: string): Observable<ImportResult> {
    return this.http.post<ImportResult>(`${this.apiUrl}/Import/search`, { areaName });
  }

  /**
   * Delete an area and all its children/climbs by name
   */
  deleteArea(areaName: string): Observable<DeleteResult> {
    return this.http.delete<DeleteResult>(`${this.apiUrl}/Import/area/${encodeURIComponent(areaName)}`);
  }
}