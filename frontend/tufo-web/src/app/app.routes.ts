import { Routes } from '@angular/router';
import { ClimbListComponent } from './climbs/climb-list';
import { ClimbDetailComponent } from './climbs/climb-detail';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'climbs' },
  { path: 'climbs', component: ClimbListComponent },
  { path: 'climbs/:id', component: ClimbDetailComponent },
];