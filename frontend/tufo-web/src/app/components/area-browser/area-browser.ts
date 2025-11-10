import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { AreasService, Area } from '../../services/areas.service';
import { AreaCardComponent } from '../area-card/area-card';

interface Breadcrumb {
  id: number;
  name: string;
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
  breadcrumbs: Breadcrumb[] = [];
  loading = false;
  error: string | null = null;

  constructor(
    private areasService: AreasService,
    private route: ActivatedRoute,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.route.paramMap.subscribe(params => {
      const areaId = params.get('id');
      if (areaId) {
        this.loadArea(+areaId);
      } else {
        this.loadTopLevelAreas();
      }
    });
  }

  loadTopLevelAreas(): void {
    this.loading = true;
    this.error = null;
    this.breadcrumbs = [];

    this.areasService.getTopLevelAreas().subscribe({
      next: (areas) => {
        // Transform top-level areas to look like an Area with subAreas
        // Now properly using the climbCount from the API
        this.currentArea = {
          id: 0,
          name: 'All Locations',
          subAreas: areas, // Use the areas directly - they already have climbCount
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
    // For now, simple breadcrumb (just current area)
    // TODO: Build full path by traversing parent hierarchy
    this.breadcrumbs = [{ id: area.id, name: area.name }];
  }

  onAreaClick(areaId: number): void {
    this.router.navigate(['/areas', areaId]);
  }

  onClimbClick(climbId: number): void {
    this.router.navigate(['/climbs', climbId]);
  }

  onBreadcrumbClick(areaId: number): void {
    if (areaId === 0) {
      this.router.navigate(['/areas']);
    } else {
      this.router.navigate(['/areas', areaId]);
    }
  }

  goBack(): void {
    if (this.currentArea?.parentAreaId) {
      this.router.navigate(['/areas', this.currentArea.parentAreaId]);
    } else {
      this.router.navigate(['/areas']);
    }
  }
}