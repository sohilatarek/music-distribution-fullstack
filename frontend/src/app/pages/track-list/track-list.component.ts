import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { TrackListStore } from '../../core/stores/track-list.store';
import { TrackStatus } from '../../core/models';

@Component({
  selector: 'app-track-list',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink],
  providers: [TrackListStore], // route-scoped: fresh state each time this view is entered
  templateUrl: './track-list.component.html',
  styleUrl: './track-list.component.css'
})
export class TrackListComponent {
  // signalStore returns a value, not a class declaration TS can use as a type,
  // so it's injected via `inject()` (the standard NGRX Signals pattern) rather
  // than as a typed constructor parameter.
  readonly store = inject(TrackListStore);

  readonly statusOptions: Array<{ label: string; value: TrackStatus | '' }> = [
    { label: 'All statuses', value: '' },
    { label: 'Draft', value: 'draft' },
    { label: 'Submitted', value: 'submitted' },
    { label: 'Distributed', value: 'distributed' }
  ];

  onStatusFilterChange(value: string): void {
    // The store's loadTracks rxMethod is bound to statusFilter in its onInit hook,
    // so patching it here is enough to trigger a refetch - no explicit "load" call.
    this.store.setStatusFilter(value as TrackStatus | '');
  }
}
