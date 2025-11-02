import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { LoginComponent } from './auth/login/login.component';
import { RegisterComponent } from './auth/register/register.component';
import { ClimbListComponent } from './climbs/climb-list';
import { ClimbDetailComponent } from './climbs/climb-detail';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },

  {
    path: '',
    canActivate: [authGuard],
    children: [
      { path: 'climbs', component: ClimbListComponent },
      { path: 'climbs/:id', component: ClimbDetailComponent },
      { path: '', pathMatch: 'full', redirectTo: 'climbs' }
    ]
  },

  { path: '**', redirectTo: '' }
];;