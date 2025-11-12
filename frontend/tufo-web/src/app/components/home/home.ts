import { Component, OnInit, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AreasService, SubArea, Area } from '../../services/areas.service';

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
export class HomeComponent implements OnInit, AfterViewInit {
  searchQuery = '';
  searchResults: SearchResult[] = [];
  searching = false;
  
  // Near Me map
  userLocation: { lat: number; lng: number } | null = null;
  loadingLocation = false;
  loadingAreas = false;
  
  // Google Maps
  private map: google.maps.Map | null = null;
  private markers: google.maps.Marker[] = [];
  private infoWindow: google.maps.InfoWindow | null = null;

  constructor(
    private areasService: AreasService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.getUserLocation();
  }

  ngAfterViewInit(): void {
    // Map will be initialized after we get user location
  }

  private waitForGoogleMaps(): Promise<void> {
    return new Promise((resolve) => {
      if (typeof google !== 'undefined' && google.maps) {
        resolve();
      } else {
        const checkInterval = setInterval(() => {
          if (typeof google !== 'undefined' && google.maps) {
            clearInterval(checkInterval);
            resolve();
          }
        }, 100);
      }
    });
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
          this.initializeMap();
          this.loadingLocation = false;
        },
        (error) => {
          console.error('Location error:', error);
          // Fallback to Austin, TX
          this.userLocation = { lat: 30.2672, lng: -97.7431 };
          this.initializeMap();
          this.loadingLocation = false;
        }
      );
    } else {
      // Fallback to Austin, TX
      this.userLocation = { lat: 30.2672, lng: -97.7431 };
      this.initializeMap();
      this.loadingLocation = false;
    }
  }

  async initializeMap(): Promise<void> {
    if (!this.userLocation) return;

    console.log('Starting map initialization...');

    // Wait for Google Maps to load
    await this.waitForGoogleMaps();
    console.log('Google Maps API loaded');

    // Add a small delay to ensure DOM is ready
    await new Promise(resolve => setTimeout(resolve, 100));

    const mapElement = document.getElementById('map');
    console.log('Map element:', mapElement);
    
    if (!mapElement) {
      console.error('Map element not found!');
      return;
    }

    console.log('Initializing map at:', this.userLocation);

    // Initialize map
    this.map = new google.maps.Map(mapElement, {
      center: this.userLocation,
      zoom: 11,
      mapTypeControl: true,
      streetViewControl: false,
      fullscreenControl: true,
      zoomControl: true,
      styles: [
        {
          featureType: 'poi',
          elementType: 'labels',
          stylers: [{ visibility: 'off' }]
        }
      ]
    });

    console.log('Map created:', this.map);

    // Initialize info window
    this.infoWindow = new google.maps.InfoWindow();

    // Add user location marker
    new google.maps.Marker({
      position: this.userLocation,
      map: this.map,
      icon: {
        path: google.maps.SymbolPath.CIRCLE,
        scale: 8,
        fillColor: '#4285F4',
        fillOpacity: 1,
        strokeColor: '#ffffff',
        strokeWeight: 2
      },
      title: 'Your Location'
    });

    console.log('User location marker added');

    // Load nearby climbing areas
    this.loadNearbyAreas();
  }

  loadNearbyAreas(): void {
    if (!this.userLocation) return;

    this.loadingAreas = true;

    this.areasService.getNearbyAreas(
      this.userLocation.lat,
      this.userLocation.lng,
      50 // 50 mile radius
    ).subscribe({
      next: (areas) => {
        console.log('Loaded areas:', areas);
        this.addAreaMarkers(areas);
        this.loadingAreas = false;
      },
      error: (err) => {
        console.error('Failed to load nearby areas:', err);
        this.loadingAreas = false;
      }
    });
  }

  addAreaMarkers(areas: Area[]): void {
    if (!this.map) return;

    console.log('Adding markers for', areas.length, 'areas');

    // Clear existing markers
    this.markers.forEach(marker => marker.setMap(null));
    this.markers = [];

    areas.forEach(area => {
      // Skip areas without coordinates
      if (!area.lat || !area.lng) return;

      const position = {
        lat: area.lat,
        lng: area.lng
      };

      // Use ClimbCount property from DTO
      const climbCount = (area as any).climbCount || 0;
      const color = this.getMarkerColorByCount(climbCount);

      console.log(`Area: ${area.name}, ClimbCount: ${climbCount}, Color: ${color}`);

      const marker = new google.maps.Marker({
        position,
        map: this.map,
        title: area.name,
        icon: {
          path: google.maps.SymbolPath.CIRCLE,
          scale: 15,
          fillColor: color,
          fillOpacity: 1,
          strokeColor: '#000000',
          strokeWeight: 3
        }
      });

      // Add click listener
      marker.addListener('click', () => {
        this.onMarkerClick(marker, area, climbCount);
      });

      this.markers.push(marker);
    });

    console.log('Added', this.markers.length, 'markers to map');
  }

  getMarkerColorByCount(climbCount: number): string {
    if (climbCount >= 100) return '#10b981'; // Green - lots of routes
    if (climbCount >= 50) return '#2196f3';  // Blue - moderate
    if (climbCount >= 20) return '#f59e0b';  // Orange - few routes
    return '#ef4444';                        // Red - very few routes
  }

  onMarkerClick(marker: google.maps.Marker, area: Area, climbCount: number): void {
    if (!this.infoWindow) return;

    const content = `
      <div style="padding: 8px; min-width: 200px;">
        <h3 style="margin: 0 0 8px 0; font-size: 16px; font-weight: 600; color: #1e293b;">
          ${area.name}
        </h3>
        <p style="margin: 0 0 12px 0; color: #64748b; font-size: 14px;">
          ${climbCount} ${climbCount === 1 ? 'route' : 'routes'}
        </p>
        <button 
          id="view-area-${area.id}"
          style="
            width: 100%;
            padding: 8px 16px;
            background: #2196f3;
            color: white;
            border: none;
            border-radius: 6px;
            font-weight: 600;
            cursor: pointer;
            font-size: 14px;
          ">
          View Area
        </button>
      </div>
    `;

    this.infoWindow.setContent(content);
    this.infoWindow.open(this.map!, marker);

    // Add click listener to button after info window opens
    google.maps.event.addListenerOnce(this.infoWindow, 'domready', () => {
      const button = document.getElementById(`view-area-${area.id}`);
      if (button) {
        button.addEventListener('click', () => {
          this.router.navigate(['/areas', area.id]);
        });
      }
    });
  }

  onSearch(): void {
    const query = this.searchQuery.trim();
    
    if (!query) {
      this.searchResults = [];
      return;
    }

    this.searching = true;

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
      this.router.navigate(['/routes', result.id]);
    }
  }

  clearSearch(): void {
    this.searchQuery = '';
    this.searchResults = [];
  }
}