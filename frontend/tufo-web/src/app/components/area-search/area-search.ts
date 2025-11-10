import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AreasService } from '../../services/areas.service';

@Component({
  selector: 'app-area-search',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './area-search.html',
  styleUrls: ['./area-search.scss']
})
export class AreaSearchComponent {
  searchQuery = '';
  deleteQuery = '';
  importing = false;
  deleting = false;
  importSuccess = false;
  deleteSuccess = false;
  importMessage = '';
  deleteMessage = '';
  error = '';

  constructor(
    private areasService: AreasService,
    private router: Router
  ) {}

  onSearch(): void {
    if (!this.searchQuery.trim()) {
      return;
    }

    this.importing = true;
    this.importSuccess = false;
    this.error = '';

    const searchTerm = this.searchQuery.trim();

    this.areasService.importLocation(searchTerm).subscribe({
      next: (result) => {
        this.importing = false;
        this.importSuccess = true;
        this.importMessage = result.message;
        
        // After import, search for the area we just imported to get its ID
        setTimeout(() => {
          this.areasService.searchAreas(searchTerm).subscribe({
            next: (areas) => {
              if (areas && areas.length > 0) {
                // Navigate to the first matching area
                this.router.navigate(['/areas', areas[0].id]);
              } else {
                // Fallback to all areas if search doesn't find it
                this.router.navigate(['/areas']);
              }
              this.importSuccess = false;
              this.searchQuery = '';
            },
            error: () => {
              // Fallback to all areas if search fails
              this.router.navigate(['/areas']);
              this.importSuccess = false;
              this.searchQuery = '';
            }
          });
        }, 1500);
      },
      error: (err) => {
        this.importing = false;
        this.error = err.error?.message || 'Failed to import location. Please try again.';
        console.error(err);
      }
    });
  }

  onDelete(): void {
    if (!this.deleteQuery.trim()) {
      return;
    }

    this.deleting = true;
    this.deleteSuccess = false;
    this.error = '';

    const areaName = this.deleteQuery.trim();

    this.areasService.deleteArea(areaName).subscribe({
      next: (result) => {
        this.deleting = false;
        this.deleteSuccess = true;
        this.deleteMessage = result.message;
        this.deleteQuery = '';
        
        // Clear success message after 3 seconds
        setTimeout(() => {
          this.deleteSuccess = false;
        }, 3000);
      },
      error: (err) => {
        this.deleting = false;
        this.error = err.error?.message || 'Failed to delete area. Please try again.';
        console.error(err);
      }
    });
  }
}