$(function () {
    if (alerts && alerts.length) {
        alerts.forEach(function (a) {
            toastr.options = {
                "closeButton": true,
                "progressBar": true,
                "positionClass": "toast-top-right",
                "timeOut": a.Duration || 5000
            };
            toastr[a.Type](a.Message, a.Title || '');
        });
    }
});
