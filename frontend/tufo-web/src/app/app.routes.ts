import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { LoginComponent } from './auth/login/login.component';
import { RegisterComponent } from './auth/register/register.component';
import { ClimbListComponent } from './climbs/climb-list';
import { ClimbDetailComponent } from './climbs/climb-detail';
import { AreaSearchComponent } from './components/area-search/area-search';
import { AreaBrowserComponent } from './components/area-browser/area-browser';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },

  {
    path: '',
    canActivate: [authGuard],
    children: [
      { path: 'search', component: AreaSearchComponent },
      { path: 'areas', component: AreaBrowserComponent },
      { path: 'areas/:id', component: AreaBrowserComponent },
      { path: 'climbs', component: ClimbListComponent },
      { path: 'climbs/:id', component: ClimbDetailComponent },
      { path: '', pathMatch: 'full', redirectTo: 'search' }  // Changed to 'search'
    ]
  },

  { path: '**', redirectTo: '' }
];