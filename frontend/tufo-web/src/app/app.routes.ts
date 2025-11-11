import { Routes } from '@angular/router';
import { authGuard } from './auth/auth.guard';
import { LoginComponent } from './auth/login/login.component';
import { RegisterComponent } from './auth/register/register.component';
import { HomeComponent } from './components/home/home';
import { AreaBrowserComponent } from './components/area-browser/area-browser';
import { ClimbDetailComponent } from './routes/climb-detail';

export const routes: Routes = [
  { path: 'login', component: LoginComponent },
  { path: 'register', component: RegisterComponent },

  {
    path: '',
    canActivate: [authGuard],
    children: [
      { path: 'home', component: HomeComponent },
      { path: 'areas', component: AreaBrowserComponent },
      { path: 'areas/state/:state', component: AreaBrowserComponent },
      { path: 'areas/:id', component: AreaBrowserComponent },
      { path: 'routes/:id', component: ClimbDetailComponent },
      { path: '', pathMatch: 'full', redirectTo: 'home' }
    ]
  },

  { path: '', redirectTo: '/login', pathMatch: 'full' },
];