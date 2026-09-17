import { HttpErrorResponse } from '@angular/common/http';
import { computed, inject } from '@angular/core';
import { rxMethod } from '@ngrx/signals/rxjs-interop';
import { patchState, signalStore, withComputed, withMethods, withState } from '@ngrx/signals';
import { tapResponse } from '@ngrx/operators';
import { forkJoin, pipe, switchMap, tap } from 'rxjs';
import { ApiService } from '../api.service';
import { Dsp, TrackDetail, TrackStatus } from '../models';

interface TrackDetailState {
  trackId: number | null;
  track: TrackDetail | null;
  dsps: Dsp[];
  selectedDsps: number[];
  loading: boolean;
  error: string | null;
  actionError: string | null;
  submitting: boolean;
}

const initialState: TrackDetailState = {
  trackId: null,
  track: null,
  dsps: [],
  selectedDsps: [],
  loading: true,
  error: null,
  actionError: null,
  submitting: false
};

/**
 * NGRX Signal Store for the track detail view: the track itself, the DSP catalog,
 * and the in-progress "distribute to these DSPs" selection. Provided per-route
 * (see TrackDetailComponent's `providers`) so each track visited gets fresh state.
 */
export const TrackDetailStore = signalStore(
  withState(initialState),

  withComputed(({ track, dsps }) => ({
    undistributedDsps: computed(() => {
      const currentTrack = track();
      if (!currentTrack) return [];
      return dsps().filter((d) => !currentTrack.distributions.some((dist) => dist.dspId === d.id));
    })
  })),

  withMethods((store, api = inject(ApiService)) => ({
    toggleDsp(dspId: number): void {
      const current = store.selectedDsps();
      patchState(store, {
        selectedDsps: current.includes(dspId)
          ? current.filter((id) => id !== dspId)
          : [...current, dspId]
      });
    },

    loadTrack: rxMethod<number>(
      pipe(
        tap((trackId) => patchState(store, { trackId, loading: true, error: null })),
        switchMap((trackId) =>
          forkJoin({
            track: api.getTrack(trackId),
            dsps: api.getDsps()
          }).pipe(
            tapResponse({
              next: ({ track, dsps }) =>
                patchState(store, { track, dsps, loading: false, submitting: false }),
              error: (err: HttpErrorResponse) =>
                patchState(store, {
                  error: err.error?.message ?? 'Failed to load track',
                  loading: false,
                  submitting: false
                })
            })
          )
        )
      )
    )
  })),

  // Separate withMethods block: `distribute`/`changeStatus` call `store.loadTrack`,
  // and a method can only see sibling methods added in an *earlier* withMethods
  // call, not ones defined alongside it in the same object literal.
  withMethods((store, api = inject(ApiService)) => ({
    distribute: rxMethod<void>(
      pipe(
        tap(() => patchState(store, { submitting: true, actionError: null })),
        switchMap(() => {
          const trackId = store.trackId();
          const dspIds = store.selectedDsps();
          if (trackId === null || dspIds.length === 0) {
            patchState(store, { submitting: false });
            return [];
          }
          return api.distributeTrack(trackId, dspIds).pipe(
            tapResponse({
              next: (track) => patchState(store, { track, selectedDsps: [], submitting: false }),
              error: (err: HttpErrorResponse) =>
                patchState(store, {
                  actionError: err.error?.message ?? 'Failed to distribute track',
                  submitting: false
                })
            })
          );
        })
      )
    ),

    changeStatus: rxMethod<TrackStatus>(
      pipe(
        tap(() => patchState(store, { submitting: true, actionError: null })),
        switchMap((status) => {
          const trackId = store.trackId();
          if (trackId === null) return [];
          return api.updateTrackStatus(trackId, status).pipe(
            tapResponse({
              next: () => {
                // Re-pull the full detail (with distributions) rather than patching
                // in the partial TrackListItem the status endpoint returns.
                patchState(store, { submitting: false });
                store.loadTrack(trackId);
              },
              error: (err: HttpErrorResponse) =>
                patchState(store, {
                  actionError: err.error?.message ?? 'Failed to update status',
                  submitting: false
                })
            })
          );
        })
      )
    )
  }))
);
