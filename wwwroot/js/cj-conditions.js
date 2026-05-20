// Generic conditional-visibility engine driven by HTML data attributes.
//
//   <div data-cj-when="Rail=fps,chaps">             show when Rail ∈ {fps, chaps}
//   <div data-cj-when-not="EntityType=individual"> show when EntityType != individual
//   <div data-cj-when-all="Rail=swift;EntType=corp"> AND-compound: show when both match
//
// Behaviour:
//   - Listens for change on every watch field.
//   - Sets style.display = "none" / "" on the wrapper.
//   - Hidden wrappers also get disabled inputs/selects so they don't submit stale values.
//   - jquery.validate.unobtrusive's default ignore: ":hidden" skips validation in hidden blocks.
//   - Works with Tom Select-wrapped selects (Tom Select dispatches 'change' on the underlying select).
(function () {
    function parseRules(raw) {
        // "Rail=fps,chaps;EntityType=individual" -> [{field:'Rail', values:['fps','chaps']}, ...]
        if (!raw) return [];
        return raw.split(";").map(function (g) {
            var pair = g.split("=");
            if (pair.length < 2) return null;
            return {
                field: pair[0].trim(),
                values: pair[1].split(",").map(function (s) { return s.trim().toLowerCase(); })
            };
        }).filter(Boolean);
    }

    function readField(form, name) {
        if (!form || !name) return "";
        var el = form.elements[name];
        if (!el) {
            var alt = form.querySelector('[name="' + name + '"]');
            if (!alt) return "";
            el = alt;
        }
        if (el.length && !el.tagName) {
            for (var i = 0; i < el.length; i++) {
                if (el[i].checked) return el[i].value;
            }
            return "";
        }
        return (el.value || "").toString().toLowerCase();
    }

    function evalRules(form, rules, requireAll) {
        if (!rules.length) return true;
        var fn = requireAll ? Array.prototype.every : Array.prototype.some;
        return fn.call(rules, function (r) {
            return r.values.indexOf(readField(form, r.field)) >= 0;
        });
    }

    function applyOne(wrapper) {
        var form = wrapper.closest("form");
        if (!form) return;

        var whenRules = parseRules(wrapper.getAttribute("data-cj-when"));
        var whenAllRules = parseRules(wrapper.getAttribute("data-cj-when-all"));
        var whenNotRules = parseRules(wrapper.getAttribute("data-cj-when-not"));

        var show = true;
        if (whenRules.length) show = show && evalRules(form, whenRules, false);
        if (whenAllRules.length) show = show && evalRules(form, whenAllRules, true);
        if (whenNotRules.length) show = show && !evalRules(form, whenNotRules, false);

        wrapper.style.display = show ? "" : "none";
        wrapper.classList.toggle("cj-field-hidden", !show);

        // Disable inputs/selects in hidden wrappers so they don't post values (keeps server VM clean).
        // We also strip the disabled flag from previously-disabled inputs the engine itself disabled.
        var controls = wrapper.querySelectorAll("input, select, textarea");
        controls.forEach(function (c) {
            if (!show) {
                if (!c.dataset.cjOrigDisabled) c.dataset.cjOrigDisabled = c.disabled ? "1" : "0";
                c.disabled = true;
            } else if (c.dataset.cjOrigDisabled !== undefined) {
                c.disabled = c.dataset.cjOrigDisabled === "1";
            }
        });
    }

    function collectWatchedFields(form) {
        var set = {};
        form.querySelectorAll("[data-cj-when], [data-cj-when-all], [data-cj-when-not]").forEach(function (w) {
            ["data-cj-when", "data-cj-when-all", "data-cj-when-not"].forEach(function (a) {
                parseRules(w.getAttribute(a)).forEach(function (r) { set[r.field] = true; });
            });
        });
        return Object.keys(set);
    }

    function initForm(form) {
        if (form.dataset.cjCondInit === "1") return;
        form.dataset.cjCondInit = "1";

        function applyAll() {
            form.querySelectorAll("[data-cj-when], [data-cj-when-all], [data-cj-when-not]")
                .forEach(applyOne);
        }

        var watched = collectWatchedFields(form);
        watched.forEach(function (name) {
            var el = form.elements[name];
            if (!el) return;
            if (el.length && !el.tagName) {
                for (var i = 0; i < el.length; i++) {
                    el[i].addEventListener("change", applyAll);
                }
            } else {
                el.addEventListener("change", applyAll);
            }
        });

        // Defer first application so Tom Select has time to initialize its values.
        setTimeout(applyAll, 60);
    }

    function initAll() {
        document.querySelectorAll("form").forEach(initForm);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initAll);
    } else {
        initAll();
    }

    window.cjConditions = { init: initAll };
})();
