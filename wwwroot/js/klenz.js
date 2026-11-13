/* KLENZ app behaviour: theme, navigation, lists, search and reminders.
   Loaded on every page after jQuery, Bootstrap and DataTables. */
(function () {
    'use strict';

    var all = function (s, r) { return Array.prototype.slice.call((r || document).querySelectorAll(s)); };
    var one = function (s, r) { return (r || document).querySelector(s); };
    var isPhone = function () { return window.matchMedia('(max-width: 860px)').matches; };
    var esc = function (s) {
        return String(s == null ? '' : s).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    };

    /* ---- Theme ---------------------------------------------------------- */
    function initTheme() {
        var btn = one('#themeBtn');
        if (!btn) { return; }
        var paint = function () {
            var dark = document.documentElement.getAttribute('data-theme') === 'dark';
            btn.innerHTML = '<i class="bi bi-' + (dark ? 'sun' : 'moon') + '"></i>';
            btn.setAttribute('aria-label', dark ? 'Switch to light theme' : 'Switch to dark theme');
        };
        paint();
        btn.addEventListener('click', function () {
            var dark = document.documentElement.getAttribute('data-theme') === 'dark';
            var next = dark ? 'light' : 'dark';
            document.documentElement.setAttribute('data-theme', next);
            try { localStorage.setItem('klenz-theme', next); } catch (e) { /* private mode */ }
            paint();
            document.dispatchEvent(new CustomEvent('klenz:theme'));
        });
    }

    /* ---- Sidebar, scrim, overlays ---------------------------------------- */
    var side, scrim, pal, pop;
    function closeAll() {
        [side, pal, pop].forEach(function (el) { if (el) { el.classList.remove('open', 'on'); } });
        if (scrim) { scrim.classList.remove('on'); }
    }
    function initShell() {
        side = one('#side'); scrim = one('#scrim'); pal = one('#pal'); pop = one('#remPop');
        all('[data-menu]').forEach(function (b) {
            b.addEventListener('click', function () {
                var open = side && !side.classList.contains('open');
                closeAll();
                if (open) { side.classList.add('open'); scrim.classList.add('on'); }
            });
        });
        if (scrim) { scrim.addEventListener('click', closeAll); }
        document.addEventListener('keydown', function (e) {
            if (e.key === 'Escape') { closeAll(); }
        });

        // Forms get a focused layout on phones: no tab bar, actions pinned to the bottom
        if (one('main form [type="submit"]') && !one('main table.datatable')) {
            document.body.classList.add('has-form');
        }

        // The + in the tab bar creates whatever the current list holds
        var fab = one('#fab'), primary = one('[data-primary-action]');
        if (fab && primary) {
            fab.setAttribute('href', primary.getAttribute('href'));
            fab.setAttribute('aria-label', primary.textContent.trim());
        }

        // Number keypad for amounts on phones
        all('input[data-val-number]').forEach(function (i) {
            if (i.type === 'text') { i.setAttribute('inputmode', 'decimal'); }
        });

        document.addEventListener('submit', function (e) {
            var msg = e.target.getAttribute && e.target.getAttribute('data-confirm');
            if (msg && !window.confirm(msg)) { e.preventDefault(); }
        });
    }

    /* ---- Lists ------------------------------------------------------------ */
    function tone(text) {
        var t = text.toLowerCase();
        if (/inactive|^no$/.test(t)) { return 'gray'; }
        if (/positive|won|approved|confirm|complete|success|order|active|^yes$/.test(t)) { return 'green'; }
        if (/lost|reject|cancel|closed|dropped|negative|fail/.test(t)) { return 'red'; }
        if (/follow|pending|wait|hold|progress|process|negotiat/.test(t)) { return 'amber'; }
        if (/quot|sent|submitted|offer/.test(t)) { return 'violet'; }
        if (/new|open|enquir|received/.test(t)) { return 'blue'; }
        return 'gray';
    }
    var MONEY = /^[^\d\-]{0,4}\s?-?\d[\d,]*(\.\d+)?$/;
    var DMY = /^(\d{2})\/(\d{2})\/(\d{4})(?: (\d{2}):(\d{2}))?$/;
    var plain = function (d) { return String(d == null ? '' : d).replace(/<[^>]*>/g, '').trim(); };
    var blank = function (t) { return t === '' || t === 'N/A' || t === 'No File'; };

    function enhanceRows(table) {
        var heads = all('thead th', table).map(function (th) { return th.textContent.trim(); });
        var statusIdx = -1;
        heads.forEach(function (h, i) { if (statusIdx < 0 && /^(current )?status$/i.test(h)) { statusIdx = i; } });

        all('tbody tr', table).forEach(function (tr) {
            if (tr.getAttribute('data-kz') || one('td.dataTables_empty', tr)) { return; }
            tr.setAttribute('data-kz', '1');
            var cells = Array.prototype.slice.call(tr.children), lead = null, actions = null, extra = 0;

            cells.forEach(function (td, i) {
                td.setAttribute('data-label', heads[i] || '');
                if (td.classList.contains('action-column')) {
                    actions = td;
                    all('a[title]', td).forEach(function (a) {
                        a.setAttribute('aria-label', a.title);
                        a.insertAdjacentHTML('beforeend', '<span class="act-label">' + esc(a.title) + '</span>');
                    });
                    return;
                }
                var text = td.textContent.trim();
                if (blank(text)) { td.classList.add('is-empty'); return; }
                if (!lead && one('strong', td)) { lead = td; }
                if (i === statusIdx) {
                    td.classList.add('is-status');
                    if (!td.children.length) { td.innerHTML = '<span class="pill pill-' + tone(text) + '">' + esc(text) + '</span>'; }
                } else if (MONEY.test(text)) {
                    td.classList.add('is-num');
                }
            });

            if (!lead) { lead = cells.filter(function (td) { return td !== actions && !td.classList.contains('is-empty'); })[0]; }
            if (lead) { lead.classList.add('is-lead'); }

            // On phones each row is a card: show the first few facts, tuck the rest behind "More"
            cells.forEach(function (td) {
                if (td === actions || td === lead || td.classList.contains('is-empty') || td.classList.contains('is-status')) { return; }
                extra += 1;
                if (extra > 3) { td.classList.add('is-more'); }
            });
            if (extra > 3 && !actions) {
                all('.is-more', tr).forEach(function (td) { td.classList.remove('is-more'); });
            } else if (extra > 3) {
                var holder = one('div', actions) || actions;
                holder.insertAdjacentHTML('afterbegin',
                    '<button type="button" class="card-toggle" aria-expanded="false"><i class="bi bi-chevron-down"></i><span>More</span></button>');
            }
        });
    }

    function buildStatusChips(api, table, wrapper) {
        var host = one('.filter-chips', wrapper);
        if (!host) { return; }
        var heads = all('thead th', table).map(function (th) { return th.textContent.trim(); });
        var idx = -1;
        heads.forEach(function (h, i) { if (idx < 0 && /^(current )?status$/i.test(h)) { idx = i; } });
        if (idx < 0) { return; }

        var counts = {}, total = 0;
        api.column(idx).data().each(function (d) {
            var t = plain(d);
            if (blank(t)) { return; }
            counts[t] = (counts[t] || 0) + 1; total += 1;
        });
        var names = Object.keys(counts);
        if (names.length < 2 || names.length > 8) { return; }

        var html = '<button type="button" class="chip on" data-status="">All<b>' + api.rows().count() + '</b></button>';
        names.sort().forEach(function (n) {
            html += '<button type="button" class="chip" data-status="' + esc(n) + '">' + esc(n) + '<b>' + counts[n] + '</b></button>';
        });
        host.innerHTML = html;
        host.addEventListener('click', function (e) {
            var chip = e.target.closest('.chip');
            if (!chip) { return; }
            all('.chip', host).forEach(function (c) { c.classList.toggle('on', c === chip); });
            var v = chip.getAttribute('data-status');
            var rx = v ? '^\\s*' + v.replace(/[.*+?^${}()|[\]\\]/g, '\\$&') + '\\s*$' : '';
            api.column(idx).search(rx, true, false).draw();
        });
    }

    function initTables() {
        var $ = window.jQuery;
        if (!$ || !$.fn.DataTable) { return; }
        var dt = $.fn.dataTable;

        // Sort dd/MM/yyyy dates and rupee amounts by value, not as text
        dt.ext.type.detect.unshift(function (d) {
            var t = plain(d);
            return blank(t) || DMY.test(t) ? 'date-dmy' : null;
        });
        dt.ext.type.order['date-dmy-pre'] = function (d) {
            var m = DMY.exec(plain(d));
            return m ? Number(m[3] + m[2] + m[1] + (m[4] || '00') + (m[5] || '00')) : 0;
        };
        dt.ext.type.detect.unshift(function (d) {
            var t = plain(d);
            return blank(t) || (MONEY.test(t) && /[^\d.\-]/.test(t)) ? 'money' : null;
        });
        dt.ext.type.order['money-pre'] = function (d) {
            var n = parseFloat(plain(d).replace(/[^\d.\-]/g, ''));
            return isNaN(n) ? 0 : n;
        };

        $('table.datatable').each(function () {
            var table = this;
            if ($.fn.DataTable.isDataTable(table)) { $(table).DataTable().destroy(); }
            $(table).DataTable({
                paging: true, searching: true, info: true, lengthChange: true, ordering: true, order: [],
                autoWidth: false, pageLength: isPhone() ? 10 : 25, lengthMenu: [10, 25, 50, 100],
                dom: "<'dt-top'lf><'filter-chips'>t<'dt-bottom'ip>",
                columnDefs: [{ targets: 'action-column', orderable: false, searchable: false }],
                language: {
                    search: '', searchPlaceholder: 'Search this list', lengthMenu: 'Show _MENU_',
                    info: '_START_ to _END_ of _TOTAL_', infoEmpty: 'No records', infoFiltered: '(of _MAX_)',
                    zeroRecords: 'Nothing matches. Clear the search or filter.',
                    emptyTable: 'Nothing here yet. Add the first record.',
                    paginate: { previous: '‹', next: '›' }
                },
                drawCallback: function () { enhanceRows(table); },
                initComplete: function () {
                    var api = this.api(), wrapper = table.closest('.dataTables_wrapper');
                    buildStatusChips(api, table, wrapper);
                    var search = one('.dataTables_filter input', wrapper);
                    if (search) { search.setAttribute('aria-label', 'Search this list'); search.setAttribute('type', 'search'); }
                    var count = one('[data-count]');
                    if (count && !count.textContent) {
                        var n = api.rows().count();
                        count.textContent = n + (n === 1 ? ' record' : ' records');
                    }
                }
            });
        });

        document.addEventListener('click', function (e) {
            var t = e.target.closest('.card-toggle');
            if (!t) { return; }
            var tr = t.closest('tr'), open = tr.classList.toggle('open');
            t.setAttribute('aria-expanded', open ? 'true' : 'false');
            one('span', t).textContent = open ? 'Less' : 'More';
        });
    }

    /* ---- Search everything (Ctrl/Cmd + K) ---------------------------------- */
    function initPalette() {
        if (!pal) { return; }
        var input = one('input', pal), list = one('.pal-list', pal), timer = null, seq = 0;
        var pages = all('.side-nav a').map(function (a) {
            var i = one('i', a);
            return { label: a.textContent.trim(), href: a.getAttribute('href'), icon: i ? i.className : 'bi bi-arrow-right', create: a.getAttribute('data-create'), noun: a.getAttribute('data-noun') };
        });

        function row(item) {
            return '<a href="' + esc(item.href) + '"><i class="' + esc(item.icon) + '"></i>' + esc(item.label) +
                (item.hint ? '<span>' + esc(item.hint) + '</span>' : '') + '</a>';
        }
        function paint(records) {
            var q = input.value.trim().toLowerCase();
            var hit = function (s) { return !q || s.toLowerCase().indexOf(q) >= 0; };
            var create = pages.filter(function (p) { return p.create && hit('new ' + p.noun); })
                .map(function (p) { return { label: 'New ' + p.noun, href: p.create, icon: 'bi bi-plus-lg' }; });
            var go = pages.filter(function (p) { return hit(p.label); });
            var html = '';
            if (records && records.length) { html += '<h5>Enquiries</h5>' + records.map(row).join(''); }
            if (create.length) { html += '<h5>Create</h5>' + create.slice(0, q ? 8 : 4).map(row).join(''); }
            if (go.length) { html += '<h5>Go to</h5>' + go.map(row).join(''); }
            list.innerHTML = html || '<div class="empty"><i class="bi bi-search"></i>No matches. Try a company name or a page.</div>';
            var first = one('a', list);
            if (first) { first.classList.add('sel'); }
        }
        function lookup() {
            var q = input.value.trim(), url = pal.getAttribute('data-search'), my = ++seq;
            if (q.length < 2 || !url) { return; }
            fetch(url + '?q=' + encodeURIComponent(q), { credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
                .then(function (r) { return r.ok ? r.json() : []; })
                .then(function (rows) {
                    if (my !== seq) { return; }
                    paint((rows || []).map(function (r) {
                        return { label: r.company || 'Enquiry', href: pal.getAttribute('data-details') + '/' + r.id, icon: 'bi bi-chat-left-text', hint: [r.details, r.status].filter(Boolean).join(', ') };
                    }));
                })
                .catch(function () { /* search is a convenience; the page list still works */ });
        }
        function open() {
            closeAll();
            pal.classList.add('on'); scrim.classList.add('on');
            input.value = ''; paint();
            setTimeout(function () { input.focus(); }, 40);
        }
        function move(step) {
            var items = all('a', list), i = items.indexOf(one('a.sel', list));
            if (!items.length) { return; }
            if (i >= 0) { items[i].classList.remove('sel'); }
            i = (i + step + items.length) % items.length;
            items[i].classList.add('sel');
            items[i].scrollIntoView({ block: 'nearest' });
        }

        all('[data-search-open]').forEach(function (b) { b.addEventListener('click', open); });
        input.addEventListener('input', function () {
            paint();
            clearTimeout(timer);
            timer = setTimeout(lookup, 250);
        });
        input.addEventListener('keydown', function (e) {
            if (e.key === 'ArrowDown') { e.preventDefault(); move(1); }
            else if (e.key === 'ArrowUp') { e.preventDefault(); move(-1); }
            else if (e.key === 'Enter') { var s = one('a.sel', list); if (s) { window.location.href = s.href; } }
        });
        document.addEventListener('keydown', function (e) {
            if ((e.metaKey || e.ctrlKey) && e.key.toLowerCase() === 'k') {
                e.preventDefault();
                if (pal.classList.contains('on')) { closeAll(); } else { open(); }
            }
        });
    }

    /* ---- Follow-up reminders (bell) ------------------------------------------ */
    function initReminders() {
        var bell = one('#bell');
        if (!bell || !pop) { return; }
        var body = one('.rows', pop), url = bell.getAttribute('data-url'), details = bell.getAttribute('data-details');

        bell.addEventListener('click', function (e) {
            if (e.target.closest('#remPop')) { return; }
            var open = !pop.classList.contains('on');
            closeAll();
            pop.classList.toggle('on', open);
        });
        document.addEventListener('click', function (e) {
            if (!e.target.closest('#bell')) { pop.classList.remove('on'); }
        });
        if (!url) { return; }

        fetch(url, { credentials: 'same-origin', headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (r) { return r.ok ? r.json() : []; })
            .then(function (rows) {
                rows = rows || [];
                var urgent = rows.filter(function (r) { return r.state !== 'soon'; }).length;
                bell.classList.toggle('has-items', urgent > 0);
                bell.setAttribute('aria-label', urgent ? urgent + ' follow-ups need attention' : 'Follow-up reminders');
                if (!rows.length) {
                    body.innerHTML = '<div class="empty"><i class="bi bi-check2-circle"></i>No follow-ups due. You are all caught up.</div>';
                    return;
                }
                body.innerHTML = rows.map(function (r) {
                    var label = r.state === 'over' ? 'Overdue' : r.state === 'today' ? 'Today' : r.when;
                    return '<a href="' + esc(details + '/' + r.id) + '"><span class="when ' + esc(r.state) + '">' + esc(label) + '</span>' +
                        '<div class="t"><strong>' + esc(r.company || 'Enquiry') + '</strong>' +
                        (r.details ? '<small>' + esc(r.details) + '</small>' : '') +
                        (r.place ? '<small>' + esc(r.place) + '</small>' : '') + '</div></a>';
                }).join('');
            })
            .catch(function () {
                body.innerHTML = '<div class="empty">Reminders could not be loaded. Refresh to try again.</div>';
            });
    }

    /* ---- Excel and PDF export: load the heavy libraries only on pages that export ---- */
    function initExports() {
        var buttons = all('[id^="exportExcel"], [id^="exportPdf"]');
        if (!buttons.length) { return; }
        var libs = [
            'https://cdnjs.cloudflare.com/ajax/libs/exceljs/4.3.0/exceljs.min.js',
            'https://cdnjs.cloudflare.com/ajax/libs/FileSaver.js/2.0.5/FileSaver.min.js',
            'https://cdnjs.cloudflare.com/ajax/libs/jspdf/2.5.1/jspdf.umd.min.js',
            'https://cdnjs.cloudflare.com/ajax/libs/jspdf-autotable/3.5.31/jspdf.plugin.autotable.min.js'
        ];
        buttons.forEach(function (b) { b.disabled = true; });
        (function next(i) {
            if (i >= libs.length) { buttons.forEach(function (b) { b.disabled = false; }); return; }
            var s = document.createElement('script');
            s.src = libs[i];
            s.onload = function () { next(i + 1); };
            s.onerror = function () { buttons.forEach(function (b) { b.title = 'Export is unavailable offline'; }); };
            document.head.appendChild(s);
        })(0);
    }

    document.addEventListener('DOMContentLoaded', function () {
        initTheme();
        initShell();
        initTables();
        initExports();
        initPalette();
        initReminders();
    });
})();
