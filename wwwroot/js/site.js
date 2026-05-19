(function () {
    document.addEventListener('click', function (e) {
        var btn = e.target.closest('.cj-copy-btn');
        if (!btn) return;
        var targetId = btn.getAttribute('data-copy-target');
        var input = targetId ? document.getElementById(targetId) : null;
        if (!input) return;
        var value = input.value || '';
        if (!value) return;
        var done = function () {
            var prev = btn.textContent;
            btn.textContent = '✓';
            setTimeout(function () { btn.textContent = prev; }, 1200);
        };
        if (navigator.clipboard && navigator.clipboard.writeText) {
            navigator.clipboard.writeText(value).then(done, done);
        } else {
            input.removeAttribute('readonly');
            input.select();
            try { document.execCommand('copy'); } catch (err) { /* ignore */ }
            input.setAttribute('readonly', 'readonly');
            done();
        }
    });
})();
