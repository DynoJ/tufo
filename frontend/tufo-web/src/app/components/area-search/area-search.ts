import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AreasService } from '../../services/areas.service';
import { DomSanitizer, SafeResourceUrl } from '@angular/platform-browser';

@Component({
  selector: 'app-area-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './area-search.html',
  styleUrls: ['./area-search.scss']
})
export class AreaSearchComponent {
  searchQuery = '';
  importing = false;
  error = '';
  
  showMap = false;
  selectedLocation: { name: string; lat: number; lng: number } | null = null;
  mapUrl: SafeResourceUrl | null = null;

  constructor(
    private areasService: AreasService,
    private router: Router,
    private sanitizer: DomSanitizer
  ) {}

  onSearchInput(): void {
    const query = this.searchQuery.trim().toLowerCase();
    
    if (!query) {
      this.showMap = false;
      return;
    }

    const locations: { [key: string]: { lat: number; lng: number } } = {
      'barton creek greenbelt': { lat: 30.2505, lng: -97.7998 },
      'reimers ranch': { lat: 30.3532, lng: -98.1242 },
      'enchanted rock': { lat: 30.5047, lng: -98.8167 },
      'hueco tanks': { lat: 31.9219, lng: -106.0453 }
    };

    const match = Object.keys(locations).find(key => key.includes(query));
    
    if (match) {
      const coords = locations[match];
      this.selectedLocation = { 
        name: match.split(' ').map(w => w.charAt(0).toUpperCase() + w.slice(1)).join(' '),
        ...coords
      };
      
      const embedUrl = `https://www.google.com/maps/embed/v1/place?key=AIzaSyBFw0Qbyq9zTFTd-tUY6dZWTgaQzuU17R8&q=${coords.lat},${coords.lng}&zoom=13`;
      this.mapUrl = this.sanitizer.bypassSecurityTrustResourceUrl(embedUrl);
      this.showMap = true;
    } else {
      this.showMap = false;
    }
  }

  getDirections(): void {
    if (this.selectedLocation) {
      window.open(
        `https://www.google.com/maps/dir/?api=1&destination=${this.selectedLocation.lat},${this.selectedLocation.lng}`,
        '_blank'
      );
    }
  }

  onImport(): void {
    if (!this.searchQuery.trim()) return;

    this.importing = true;
    this.error = '';

    const searchTerm = this.searchQuery.trim();

    this.areasService.importLocation(searchTerm).subscribe({
      next: (result) => {
        this.importing = false;
        
        this.areasService.searchAreas(searchTerm).subscribe({
          next: (areas) => {
            this.router.navigate(areas?.length > 0 ? ['/areas', areas[0].id] : ['/areas']);
          },
          error: () => {
            this.router.navigate(['/areas']);
          }
        });
      },
      error: (err) => {
        this.importing = false;
        this.error = err.error?.message || 'Failed to import location.';
      }
    });
  }
}