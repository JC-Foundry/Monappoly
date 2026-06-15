// Game-play hub coordinator.
//
// Owns the single SignalR connection to the game-play hub for an in-game page
// and fans engine prompts out to registered per-prompt handlers (one module per
// prompt type, under /scripts/InGame/Prompts/). One connection per page —
// handlers never open their own.
//
// A prompt module registers itself:
//
//   GamePlayHub.registerPrompt({
//       type: 'DiceRoll',                  // engine [JsonPolymorphic] $type it renders
//       onOpen(prompt, stamp, ctx) { },    // a prompt of this type opened
//       onClose(promptId) { },             // the open prompt closed (promptId may be
//                                          //   null on a resync that found none)
//   });
//
// ctx (passed to onOpen) exposes:
//   ctx.gameId, ctx.userId                 // page context — the profiled player
//   ctx.submit(stamp, response) -> Promise<bool>   // SubmitPrompt; false = stale/invalid
//   ctx.refresh() -> Promise               // re-pull the current prompt and re-dispatch
//
// Non-prompt consumers (e.g. a future StateChanged board projection) attach raw
// hub handlers with GamePlayHub.on(eventName, callback) — queued until the
// connection is built, then forwarded.
//
// Load order: this script must come before any /Prompts/* handler so the global
// exists when they register. Registration is synchronous at parse time; the
// connection is started once on DOMContentLoaded, by which point every handler
// loaded after this script has registered.
(function () {
    'use strict';

    const handlers = new Map();   // $type -> handler
    const observers = [];         // type-agnostic { onOpen, onClose } prompt observers
    const queued = [];            // { event, callback } raw subscriptions queued pre-connect
    const resyncHandlers = [];    // callbacks run on (re)connect / wake to re-pull live state
    let connection = null;
    let ctx = null;
    let started = false;
    let stopped = false;          // set only on intentional teardown (none today) — guards reconnect loops

    function registerPrompt(handler) {
        if (handler && handler.type) handlers.set(handler.type, handler);
    }

    // Type-agnostic prompt observer — notified for *every* prompt regardless of
    // type, through the same dispatch path as the typed handlers (so it also
    // fires on the reconnect/initial resync, not just live PromptOpened events).
    // Used by the host drawer to auto-open on the prompt's player.
    function observePrompts(observer) {
        if (observer) observers.push(observer);
    }

    function on(event, callback) {
        if (connection) connection.on(event, callback);
        else queued.push({ event, callback });
    }

    // Register a callback to run whenever the live connection is (re)established or the page
    // wakes from sleep — i.e. the moments where buffered StateChanged frames were missed. The
    // state modules (play-state / player-state / drawer) register their re-fetch here so that a
    // reconnect refreshes the *whole view*, not just the open prompt.
    function onResync(callback) {
        if (callback) resyncHandlers.push(callback);
    }

    // Invoke a hub method (e.g. a command like EndTurn). Rejects if called before
    // the connection is up.
    function invoke(method) {
        if (!connection) return Promise.reject(new Error('game-play hub not connected'));
        return connection.invoke.apply(connection, arguments);
    }

    function dispatchOpen(msg) {
        if (!msg || !msg.prompt) return;
        const handler = handlers.get(msg.prompt['$type']);
        if (handler) handler.onOpen(msg.prompt, msg.concurrencyStamp, ctx);
        observers.forEach(o => { if (o.onOpen) o.onOpen(msg.prompt, msg.concurrencyStamp, ctx); });
    }

    // Only one prompt is ever open at a time, and PromptClosed carries no type,
    // so tell every handler — each ignores a close for a prompt it isn't showing.
    function dispatchClose(promptId) {
        handlers.forEach(h => { if (h.onClose) h.onClose(promptId); });
        observers.forEach(o => { if (o.onClose) o.onClose(promptId); });
    }

    async function refresh() {
        try {
            const msg = await connection.invoke('GetCurrentPrompt');
            if (msg && msg.prompt) dispatchOpen(msg);
            else dispatchClose(null);
        } catch (e) {
            console.error('GetCurrentPrompt failed:', e);
        }
    }

    // Full re-sync after a (re)connect or wake: re-pull the open prompt AND tell every state
    // listener to re-fetch its rendered view. SignalR doesn't buffer group broadcasts for a
    // dropped client, so without this a reconnected device sits on a stale board until the next
    // live frame happens to fire. The state re-fetch is plain HTTP, so it works even while the
    // socket is still mid-reconnect.
    function resync() {
        if (connection) refresh();
        resyncHandlers.forEach(cb => { try { cb(); } catch (e) { console.error('resync handler failed:', e); } });
    }

    // Capped, never-give-up reconnect: the SignalR default ([0,2,10,30]s then STOP) leaves a
    // slept/flaky phone permanently dead after ~30s, forcing a manual reload. Retry forever,
    // backing off to a 10s ceiling, so a connection always heals on its own once the network
    // returns. Returning a number (never null) is what keeps it retrying indefinitely.
    const retryPolicy = {
        nextRetryDelayInMilliseconds: function (ctxRetry) {
            const steps = [0, 2000, 5000, 10000];
            return ctxRetry.previousRetryCount < steps.length
                ? steps[ctxRetry.previousRetryCount]
                : 10000;
        }
    };

    // Manual cold-start loop. withAutomaticReconnect only covers a connection that was once up;
    // if the very first start fails, or onclose fires (e.g. the page was hidden long enough that
    // the reconnect attempts elapsed against a suspended timer), we keep retrying ourselves.
    async function startWithRetry() {
        let delay = 2000;
        while (!stopped) {
            try {
                await connection.start();
                resync();
                return;
            } catch (err) {
                console.error('Game play hub connect failed, retrying:', err);
                await new Promise(r => setTimeout(r, delay));
                delay = Math.min(delay * 2, 10000);
            }
        }
    }

    // When a slept phone wakes, the reconnect timers were suspended while locked, so the socket
    // can be a zombie (client still thinks it's "Connected") or fully dropped. On becoming
    // visible: if we're disconnected, kick a fresh start; otherwise resync immediately — the
    // GetCurrentPrompt invoke surfaces a dead socket (triggering reconnect) while the HTTP state
    // re-fetch repaints the view now, without waiting for the keep-alive to time the zombie out.
    function onWake() {
        if (!connection || stopped) return;
        if (connection.state === signalR.HubConnectionState.Disconnected) startWithRetry();
        else resync();
    }

    // Fatal game error (GameFaulted): the server abandoned the game's pump, so
    // this session is over. Show a centred, static-backdrop alert and force-quit
    // to the home screen. Terminal — there's no dismissing it back into the game.
    function showFatalError(message) {
        if (document.getElementById('gameFaultModal')) return;   // already shown

        const text = message || 'An unexpected error occurred and the game cannot continue.';
        const modalEl = document.createElement('div');
        modalEl.className = 'modal fade';
        modalEl.id = 'gameFaultModal';
        modalEl.tabIndex = -1;
        modalEl.setAttribute('data-bs-backdrop', 'static');
        modalEl.setAttribute('data-bs-keyboard', 'false');
        modalEl.innerHTML =
            '<div class="modal-dialog modal-dialog-centered">' +
              '<div class="modal-content border border-danger border-2">' +
                '<div class="modal-header text-bg-danger border-0">' +
                  '<h5 class="modal-title"><i class="bi bi-exclamation-triangle-fill me-1"></i> Game error</h5>' +
                '</div>' +
                '<div class="modal-body"><p class="mb-0"></p></div>' +
                '<div class="modal-footer">' +
                  '<button type="button" class="btn btn-danger w-100" data-fault-leave>Return to home</button>' +
                '</div>' +
              '</div>' +
            '</div>';
        modalEl.querySelector('.modal-body p').textContent = text;
        document.body.appendChild(modalEl);

        const leave = () => { window.location.href = '/Index'; };
        modalEl.querySelector('[data-fault-leave]').addEventListener('click', leave);

        if (typeof bootstrap !== 'undefined') {
            new bootstrap.Modal(modalEl, { backdrop: 'static', keyboard: false }).show();
        } else {
            leave();   // no Bootstrap to render the modal — just bail home
        }
    }

    function start() {
        if (started) return;
        // Player profile ([data-player], carries userId) or host play page
        // ([data-play], no userId — prompt handlers there self-filter / aren't
        // registered). Only gameId is required to connect.
        const root = document.querySelector('[data-player], [data-play]');
        if (!root || typeof signalR === 'undefined') return;

        const gameId = root.dataset.gameId;
        if (!gameId) return;
        const userId = root.dataset.userId || null;

        started = true;
        connection = new signalR.HubConnectionBuilder()
            .withUrl('/hubs/game-play?gameId=' + encodeURIComponent(gameId))
            .withAutomaticReconnect(retryPolicy)
            .build();

        ctx = {
            gameId: gameId,
            userId: userId,
            submit: (stamp, response) => connection.invoke('SubmitPrompt', stamp, response),
            refresh: refresh
        };

        connection.on('PromptOpened', dispatchOpen);
        connection.on('PromptClosed', (msg) => { if (msg) dispatchClose(msg.promptId); });
        connection.on('GameFaulted', (msg) => showFatalError(msg && msg.message));
        // Game over: the server finished the game and persisted the result. Every client
        // (host tablet + phones) moves to the finished-game results page.
        connection.on('GameCompleted', (msg) => {
            const id = (msg && msg.gameId) || gameId;
            window.location.href = '/Games/Finished/' + encodeURIComponent(id);
        });
        // Host "Force Refresh": every client hard-reloads and re-fetches the current live state.
        connection.on('ForceRefresh', () => window.location.reload());
        // Host "Cancel Game": the game was abandoned — every client returns home.
        connection.on('GameCancelled', () => { window.location.href = '/Index'; });
        queued.forEach(h => connection.on(h.event, h.callback));

        // Re-pull prompt + state after a dropped connection is restored.
        connection.onreconnected(resync);
        // If the connection closes for good (rare with the infinite policy, but possible if it
        // never came up, or the page was hidden past the reconnect window), drive our own retry.
        connection.onclose(() => { if (!stopped) startWithRetry(); });

        // Wake handlers: a locked/backgrounded phone suspends the reconnect timers, so re-check
        // the connection the instant the page is shown again. pageshow also covers bfcache restores.
        document.addEventListener('visibilitychange', () => { if (document.visibilityState === 'visible') onWake(); });
        window.addEventListener('pageshow', onWake);

        startWithRetry();
    }

    window.GamePlayHub = { registerPrompt: registerPrompt, observePrompts: observePrompts, on: on, onResync: onResync, invoke: invoke };

    // Defer start until handler modules loaded after this script have registered.
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start);
    else setTimeout(start, 0);
})();