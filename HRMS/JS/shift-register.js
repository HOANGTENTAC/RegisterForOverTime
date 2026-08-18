$(function () {
    const $fromDate = $("#FromDate");
    const $toDate = $("#ToDate");

    initDateRange();

    HRMS.EmployeeConfirmSelector.init({
        formSelector: "#ShiftRegisterForm",
        renderMode: "shift",
        shiftTypeSelector: "#ShiftTypeId"
    });

    function initDateRange() {
        const today = new Date();

        const yyyy = today.getFullYear();
        const mm = String(today.getMonth() + 1).padStart(2, "0");
        const dd = String(today.getDate()).padStart(2, "0");

        const minDate = `${yyyy}-${mm}-${dd}`;

        $fromDate.attr("min", minDate);
        $toDate.attr("min", minDate);
    }

    $fromDate.on("change", function () {
        const fromValue = $fromDate.val();

        if (!fromValue) {
            return;
        }

        $toDate.attr("min", fromValue);

        if ($toDate.val() && $toDate.val() < fromValue) {
            $toDate.val(fromValue);
        }
    });

    $toDate.on("change", function () {
        const fromValue = $fromDate.val();
        const toValue = $toDate.val();

        if (!fromValue || !toValue) {
            return;
        }

        if (toValue < fromValue) {
            toastr.error("Ngày kết thúc không được nhỏ hơn ngày bắt đầu.");
            $toDate.val(fromValue);
        }
    });
});