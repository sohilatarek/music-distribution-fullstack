import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { patchState, signalStore, withComputed, withHooks, withMethods, withState } from '@ngrx/signals';
import { tapResponse } from '@ngrx/operators';
import { pipe, switchMap, tap } from 'rxjs';
import { ApiService } from '../api.service';
import { TrackListItem, TrackStatus } from '../models';

interface TrackListState {
  tracks: TrackListItem[];
  statusFilter: TrackStatus | '';
  loading: boolean;
  error: string | null;
}

const initialState: TrackListState = {
  tracks: [],
  statusFilter: '',
  loading: false,
  error: null
};

/**
 * NGRX Signal Store for the track list view.
 *
 * `loadTracks` is an rxMethod bound directly to the `statusFilter` signal in the
 * onInit hook below, so it re-runs automatically whenever the filter changes -
 * `setStatusFilter` only needs to patch the filter value, it never has to call
 * `loadTracks` itself. Provided per-route (see TrackListComponent's `providers`),
 * so its state resets whenever you navigate away and back rather than persisting
 * as an app-wide singleton.
 */
export const TrackListStore = signalStore(
  withState(initialState),

  withComputed(({ tracks }) => ({
    genreCount: computed(() => new Set(tracks().map((t) => t.genre)).size)
  })),

  withMethods((store, api = inject(ApiService)) => ({
    setStatusFilter(status: TrackStatus | ''): void {
      patchState(store, { statusFilter: status });
    },

    loadTracks: rxMethod<TrackStatus | ''>(
      pipe(
        tap(() => patchState(store, { loading: true, error: null })),
        switchMap((status) =>
          api.getTracks(status ? { status } : {}).pipe(
            tapResponse({
              next: (tracks) => patchState(store, { tracks, loading: false }),
              error: (err: HttpErrorResponse) =>
                patchState(store, {
                  error: err.error?.message ?? 'Failed to load tracks',
                  loading: false
                })
            })
          )
        )
      )
    )
  })),

  withHooks({
    onInit(store) {
      // Passing the statusFilter signal (rather than a plain value) makes this
      // reactive: every patchState({ statusFilter: ... }) call re-triggers the load.
      store.loadTracks(store.statusFilter);
    }
  })
);
