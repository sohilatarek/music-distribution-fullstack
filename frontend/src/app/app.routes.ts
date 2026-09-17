import { Routes } from '@angular/router';
import { TrackListComponent } from './pages/track-list/track-list.component';
import { TrackDetailComponent } from './pages/track-detail/track-detail.component';

export const routes: Routes = [
  { path: '', component: TrackListComponent },
  { path: 'tracks/:id', component: TrackDetailComponent },
  { path: '**', redirectTo: '' }
];
