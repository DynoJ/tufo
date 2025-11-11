import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AreasService } from '../../services/areas.service';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

interface SearchResult {
  id: number;
  name: string;
  type: 'area' | 'climb';
  location?: string;
  grade?: string;
  climbCount?: number;
}

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './home.html',
  styleUrls: ['./home.scss']
})
export class HomeComponent implements OnInit {
  searchQuery = '';
  searchResults: SearchResult[] = [];
  searching = false;
  
  // Near Me map
  userLocation: { lat: number; lng: number } | null = null;
  nearbyMapUrl: SafeResourceUrl | null = null;
  loadingLocation = false;

  constructor(
    private areasService: AreasService,
    private router: Router,
    private sanitizer: DomSanitizer
  ) {}

  ngOnInit(): void {
    this.getUserLocation();
  }

  getUserLocation(): void {
    this.loadingLocation = true;
    
    if (navigator.geolocation) {
      navigator.geolocation.getCurrentPosition(
        (position) => {
          this.userLocation = {
            lat: position.coords.latitude,
            lng: position.coords.longitude
          };
          this.loadNearbyMap();
          this.loadingLocation = false;
        },
        (error) => {
          console.error('Location error:', error);
          // Fallback to Austin, TX
          this.userLocation = { lat: 30.2672, lng: -97.7431 };
          this.loadNearbyMap();
          this.loadingLocation = false;
        }
      );
    } else {
      // Fallback to Austin, TX
      this.userLocation = { lat: 30.2672, lng: -97.7431 };
      this.loadNearbyMap();
      this.loadingLocation = false;
    }
  }

  loadNearbyMap(): void {
    if (!this.userLocation) return;

    // For now, just show a centered map - we'll add markers via backend later
    const mapUrl = `https://www.google.com/maps/embed/v1/view?key=AIzaSyBFw0Qbyq9zTFTd-tUY6dZWTgaQzuU17R8&center=${this.userLocation.lat},${this.userLocation.lng}&zoom=11`;
    this.nearbyMapUrl = this.sanitizer.bypassSecurityTrustResourceUrl(mapUrl);
  }

  onSearch(): void {
    const query = this.searchQuery.trim();
    
    if (!query) {
      this.searchResults = [];
      return;
    }

    this.searching = true;

    // Call your search API endpoint (we'll create this)
    this.areasService.search(query).subscribe({
      next: (results) => {
        this.searchResults = results;
        this.searching = false;
      },
      error: (err) => {
        console.error('Search error:', err);
        this.searching = false;
      }
    });
  }

  selectResult(result: SearchResult): void {
    if (result.type === 'area') {
      this.router.navigate(['/areas', result.id]);
    } else {
      this.router.navigate(['/climbs', result.id]);
    }
  }

  clearSearch(): void {
    this.searchQuery = '';
    this.searchResults = [];
  }
}