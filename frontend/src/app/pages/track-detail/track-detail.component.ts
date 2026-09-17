import { CommonModule } from '@angular/common';
import { Component, OnInit, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth.service';
import { TrackDetailStore } from '../../core/stores/track-detail.store';
import { TrackStatus } from '../../core/models';

@Component({
  selector: 'app-track-detail',
  standalone: true,
  imports: [CommonModule, RouterLink],
  providers: [TrackDetailStore], // route-scoped: fresh state for whichever track id is loaded
  templateUrl: './track-detail.component.html',
  styleUrl: './track-detail.component.css'
})
export class TrackDetailComponent implements OnInit {
  // signalStore returns a value, not a class declaration TS can use as a type,
  // so it's injected via `inject()` (the standard NGRX Signals pattern) rather
  // than as a typed constructor parameter.
  readonly store = inject(TrackDetailStore);
  readonly auth = inject(AuthService);
  private readonly route = inject(ActivatedRoute);

  readonly statuses: TrackStatus[] = ['draft', 'submitted', 'distributed'];

  ngOnInit(): void {
    const trackId = Number(this.route.snapshot.paramMap.get('id'));
    this.store.loadTrack(trackId);
  }

  distribute(): void {
    if (this.store.selectedDsps().length === 0) return;
    this.store.distribute();
  }

  changeStatus(status: TrackStatus): void {
    this.store.changeStatus(status);
  }
}
