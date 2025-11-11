import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { AreasService, Area, StateSummary, SubArea } from '../../services/areas.service';
import { AreaCardComponent } from '../area-card/area-card';

interface Breadcrumb {
  id: number | string;
  name: string;
  type: 'root' | 'state' | 'area';
}

@Component({
  selector: 'app-area-browser',
  standalone: true,
  imports: [CommonModule, AreaCardComponent],
  templateUrl: './area-browser.html',
  styleUrls: ['./area-browser.scss']
})
export class AreaBrowserComponent implements OnInit {
  currentArea: Area | null = null;
  states: StateSummary[] = [];
  currentState: string | null = null;
  breadcrumbs: Breadcrumb[] = [];
  loading = false;
  error: string | null = null;
  viewMode: 'states' | 'areas' | 'details' = 'states';

  constructor(
    private areasService: AreasService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const areaId = params.get('id');
      const state = params.get('state');
      
      if (areaId) {
        this.viewMode = 'details';
        this.loadArea(+areaId);
      } else if (state) {
        this.viewMode = 'areas';
        this.currentState = state;
        this.loadAreasInState(state);
      } else {
        this.viewMode = 'states';
        this.loadStates();
      }
    });
  }

  loadStates(): void {
    this.loading = true;
    this.error = null;
    this.breadcrumbs = [];

    this.areasService.getStates().subscribe({
      next: (states) => {
        this.states = states;
        // Transform states to look like SubAreas for display
        this.currentArea = {
          id: 0,
          name: 'All Locations',
          subAreas: states.map(s => ({
            id: 0, // Dummy ID for states
            name: s.state,
            climbCount: s.climbCount
          })),
          climbs: []
        };
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load states';
        this.loading = false;
        console.error(err);
      }
    });
  }

  loadAreasInState(state: string): void {
    this.loading = true;
    this.error = null;
    this.breadcrumbs = [
      { id: 0, name: 'All Locations', type: 'root' },
      { id: state, name: state, type: 'state' }
    ];

    this.areasService.getAreasInState(state).subscribe({
      next: (areas) => {
        this.currentArea = {
          id: 0,
          name: state,
          state: state,
          subAreas: areas,
          climbs: []
        };
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load areas';
        this.loading = false;
        console.error(err);
      }
    });
  }

  loadArea(id: number): void {
    this.loading = true;
    this.error = null;

    this.areasService.getAreaDetails(id).subscribe({
      next: (area) => {
        this.currentArea = area;
        this.buildBreadcrumbs(area);
        this.loading = false;
      },
      error: (err) => {
        this.error = 'Failed to load area details';
        this.loading = false;
        console.error(err);
      }
    });
  }

  buildBreadcrumbs(area: Area): void {
    // Build breadcrumb path
    this.breadcrumbs = [
      { id: 0, name: 'All Locations', type: 'root' }
    ];
    
    if (area.state) {
      this.breadcrumbs.push({ id: area.state, name: area.state, type: 'state' });
    }
    
    this.breadcrumbs.push({ id: area.id, name: area.name, type: 'area' });
  }

  onAreaClick(area: SubArea): void {
    // Check if it's a state (dummy id = 0) or actual area
    if (this.viewMode === 'states') {
      // Clicked on a state
      this.router.navigate(['/areas/state', area.name]);
    } else {
      // Clicked on an actual area
      this.router.navigate(['/areas', area.id]);
    }
  }

  onClimbClick(climbId: number): void {
    this.router.navigate(['/routes', climbId]);
  }

  onBreadcrumbClick(crumb: Breadcrumb): void {
    if (crumb.type === 'root') {
      this.router.navigate(['/areas']);
    } else if (crumb.type === 'state') {
      this.router.navigate(['/areas/state', crumb.id]);
    } else {
      this.router.navigate(['/areas', crumb.id]);
    }
  }

  goBack(): void {
    if (this.viewMode === 'details' && this.currentArea?.parentAreaId) {
      this.router.navigate(['/areas', this.currentArea.parentAreaId]);
    } else if (this.viewMode === 'details' && this.currentArea?.state) {
      this.router.navigate(['/areas/state', this.currentArea.state]);
    } else if (this.viewMode === 'areas') {
      this.router.navigate(['/areas']);
    } else {
      this.router.navigate(['/areas']);
    }
  }
}