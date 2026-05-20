// jquery.validate.unobtrusive adapter for the server-side [CjRequiredIf] attribute.
// Required attributes emitted on the input:
//   data-val-cjrequiredif="<message>"
//   data-val-cjrequiredif-field="<watch property>"
//   data-val-cjrequiredif-values="<comma-separated allowed values>"
//   data-val-cjrequiredif-alsofield="<optional second watch>"
//   data-val-cjrequiredif-alsovalues="<optional second values>"
(function ($) {
    if (!$ || !$.validator || !$.validator.unobtrusive) return;

    function fieldValue(form, name) {
        if (!form || !name) return "";
        // form.elements gives both inputs and selects (incl. Tom Select wrapped <select>).
        var el = form.elements[name];
        if (!el) {
            var byId = form.querySelector('[name="' + name + '"]');
            if (!byId) return "";
            el = byId;
        }
        if (el.length && !el.tagName) {
            // NodeList (radio group) — find checked.
            for (var i = 0; i < el.length; i++) {
                if (el[i].checked) return el[i].value;
            }
            return "";
        }
        return (el.value != null ? el.value : "").toString();
    }

    function matches(value, csv) {
        if (!csv) return false;
        var v = (value || "").toLowerCase();
        var parts = csv.split(",");
        for (var i = 0; i < parts.length; i++) {
            if (parts[i].trim().toLowerCase() === v) return true;
        }
        return false;
    }

    $.validator.addMethod("cjrequiredif", function (value, element, params) {
        var form = $(element).closest("form")[0];
        if (!form) return true;
        if (!matches(fieldValue(form, params.field), params.values)) return true;
        if (params.alsoField && !matches(fieldValue(form, params.alsoField), params.alsoValues)) return true;
        // Required at this point.
        return value != null && $.trim(value.toString()).length > 0;
    });

    $.validator.unobtrusive.adapters.add(
        "cjrequiredif",
        ["field", "values", "alsofield", "alsovalues"],
        function (options) {
            options.rules["cjrequiredif"] = {
                field: options.params.field,
                values: options.params.values,
                alsoField: options.params.alsofield,
                alsoValues: options.params.alsovalues
            };
            if (options.message) {
                options.messages["cjrequiredif"] = options.message;
            }
        }
    );
})(window.jQuery);
