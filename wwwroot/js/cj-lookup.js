(function () {
    if (typeof TomSelect === 'undefined') return;

    function initSelect(el) {
        if (el.dataset.cjLookupInit === '1') return;
        el.dataset.cjLookupInit = '1';

        var kind = el.getAttribute('data-cj-lookup') || '';
        var remote = el.getAttribute('data-cj-remote') === 'true';
        var placeholder = el.getAttribute('data-placeholder') || ('Search ' + kind + '...');
        var hasGroups = el.querySelector('optgroup') !== null;

        var settings = {
            create: false,
            allowEmptyOption: el.querySelector('option[value=""]') !== null,
            placeholder: placeholder,
            maxOptions: 500,
            searchField: ['text', 'value'],
            plugins: { clear_button: { title: 'Clear' } }
        };

        if (hasGroups) {
            var groups = {};
            el.querySelectorAll('optgroup').forEach(function (og) {
                groups[og.label] = { value: og.label, label: og.label };
            });
            settings.optgroups = Object.values(groups);
            settings.optgroupField = 'group';
            settings.lockOptgroupOrder = true;
            settings.render = Object.assign(settings.render || {}, {
                optgroup_header: function (data, escape) {
                    return '<div class="optgroup-header">' + escape(data.label) + '</div>';
                }
            });
        }

        if (remote) {
            settings.valueField = 'code';
            settings.labelField = 'label';
            settings.searchField = ['label', 'code'];
            settings.preload = 'focus';
            settings.load = function (query, callback) {
                var url = '/api/lookups/' + encodeURIComponent(kind) +
                    '?q=' + encodeURIComponent(query || '') + '&take=100';
                fetch(url, { headers: { Accept: 'application/json' } })
                    .then(function (r) { return r.ok ? r.json() : []; })
                    .then(function (items) { callback(items || []); })
                    .catch(function () { callback(); });
            };
            settings.render = Object.assign(settings.render || {}, {
                option: function (data, escape) {
                    return '<div><span class="cj-opt-label">' + escape(data.label) +
                        '</span> <span class="cj-opt-code">' + escape(data.code) + '</span></div>';
                },
                item: function (data, escape) {
                    return '<div>' + escape(data.label) + ' <span class="cj-opt-code">' + escape(data.code) + '</span></div>';
                }
            });
        }

        try {
            new TomSelect(el, settings);
        } catch (err) {
            console.warn('[cj-lookup] init failed for', kind, err);
        }
    }

    function initAll(root) {
        (root || document).querySelectorAll('select[data-cj-lookup]').forEach(initSelect);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', function () { initAll(); });
    } else {
        initAll();
    }

    // Expose for dynamic re-init (e.g. after partial refresh).
    window.cjLookup = { init: initAll };
})();
