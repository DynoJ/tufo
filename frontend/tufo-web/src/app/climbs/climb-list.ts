import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule, Router } from '@angular/router';
import { ApiService, Climb } from '../services/api';

@Component({
  selector: 'app-climb-list',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './climb-list.html',
  styleUrls: ['./climb-list.scss']
})
export class ClimbListComponent implements OnInit {
  climbs: Climb[] = [];
  loading = true;

  constructor(private api: ApiService, private router: Router) {}

  ngOnInit() {
    this.api.getClimbs().subscribe({
      next: d => { this.climbs = d; this.loading = false; },
      error: _ => { this.loading = false; }
    });
  }

  open(id: number) { this.router.navigate(['/climbs', id]); }
}