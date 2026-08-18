//$(function () {
//    // =====================================================
//    // STATE
//    // =====================================================

//    const selected = new Map();

//    const $form = $("#OverTimeForm");
//    const $date = $("#DateRequest");
//    const $fromTime = $("#FromTime");
//    const $toTime = $("#ToTime");
//    const $shift = $(".shift-options");
//    const $radios = $("input[name='Request.OvertimeType']");
//    const $confirmSelect = $("#ConfirmUserCD");
//    const $confirmHint = $("#confirmUserHint");

//    // =====================================================
//    // INIT
//    // =====================================================

//    initMinDate();
//    disableShift();
//    initSelectedEmployees();
//    renderSelectedEmployees();
//    syncConfirmUsers();

//    // Nếu project có Select2 JS thì tự init.
//    // Nếu chưa load select2.js thì đoạn này sẽ tự bỏ qua.
//    if ($.fn.select2 && $confirmSelect.length > 0) {
//        $confirmSelect.select2({
//            width: "100%",
//            placeholder: "-- Chọn người xác nhận --"
//        });
//    }

//    // =====================================================
//    // DATE / TIME
//    // =====================================================

//    function initMinDate() {
//        const yesterday = new Date();

//        yesterday.setDate(yesterday.getDate() - 1);

//        const yyyy = yesterday.getFullYear();
//        const mm = String(yesterday.getMonth() + 1).padStart(2, "0");
//        const dd = String(yesterday.getDate()).padStart(2, "0");

//        const minDate = `${yyyy}-${mm}-${dd}`;

//        $date.attr("min", minDate);
//        $fromTime.attr("min", minDate + "T00:00");
//        $toTime.attr("min", minDate + "T00:00");
//    }

//    function disableShift() {
//        $shift.removeClass("active");
//        $radios.prop("checked", false);
//        $radios.prop("disabled", true);
//    }

//    function enableShift() {
//        $shift.addClass("active");
//        $radios.prop("disabled", false);
//    }

//    function syncDateWithDateTime() {
//        const selectedDate = $date.val();

//        if (!selectedDate) {
//            return;
//        }

//        const fromValue = $fromTime.val();
//        const toValue = $toTime.val();

//        const fromTimePart =
//            fromValue && fromValue.includes("T")
//                ? fromValue.split("T")[1]
//                : "16:30";

//        const toTimePart =
//            toValue && toValue.includes("T")
//                ? toValue.split("T")[1]
//                : "19:00";

//        $fromTime.val(selectedDate + "T" + fromTimePart);
//        $toTime.val(selectedDate + "T" + toTimePart);
//    }

//    function applyHolidayShift(type) {
//        const selectedDate = $date.val();

//        if (!selectedDate) {
//            return;
//        }

//        if (type === "HanhChinh") {
//            $fromTime.val(selectedDate + "T07:50");
//            $toTime.val(selectedDate + "T16:30");
//            return;
//        }

//        if (type === "Khac") {
//            $fromTime.val(selectedDate + "T16:30");
//            $toTime.val(selectedDate + "T19:00");
//        }
//    }

//    function checkDayOff() {
//        const date = $date.val();

//        if (!date) {
//            disableShift();
//            return;
//        }

//        $.ajax({
//            url: window.checkDayOffUrl,
//            type: "GET",
//            data: {
//                date: date
//            },
//            success: function (res) {
//                if (!res || !res.success) {
//                    disableShift();
//                    return;
//                }

//                if (res.isDayOff) {
//                    enableShift();
//                    $radios.prop("checked", false);
//                } else {
//                    disableShift();
//                }
//            },
//            error: function () {
//                disableShift();
//            }
//        });
//    }

//    $date.on("change", function () {
//        syncDateWithDateTime();
//        checkDayOff();
//    });

//    $radios.on("change", function () {
//        if ($(this).is(":disabled")) {
//            return;
//        }

//        applyHolidayShift($(this).val());
//    });

//    // =====================================================
//    // EMPLOYEE HELPERS
//    // =====================================================

//    function getEmployee($el) {
//        return {
//            id: ($el.val() || "").toString(),
//            code: ($el.data("code") || "").toString(),
//            name: ($el.data("name") || "").toString(),
//            dept: ($el.data("dept") || "").toString(),
//            luuyke: $el.data("luuyke") || 0
//        };
//    }

//    function getLuuykeClass(val) {
//        const h = parseFloat(val || 0);

//        if (h >= 40) {
//            return "luuyke-danger";
//        }

//        if (h >= 30) {
//            return "luuyke-warning";
//        }

//        return "luuyke-normal";
//    }

//    function getLuuykeIcon(val) {
//        const h = parseFloat(val || 0);

//        if (h >= 40) {
//            return "⛔";
//        }

//        if (h >= 30) {
//            return "⚠️";
//        }

//        return "🟢";
//    }

//    function initSelectedEmployees() {
//        $(".employee-checkbox:checked").each(function () {
//            const emp = getEmployee($(this));

//            if (emp.id) {
//                selected.set(emp.id, emp);
//            }
//        });

//        $(".dept-content").each(function () {
//            updateCheckAllState($(this));
//        });
//    }

//    // =====================================================
//    // RENDER SELECTED EMPLOYEES
//    // =====================================================

//    function renderSelectedEmployees() {
//        const $body = $("#selectedBody");

//        if ($body.length === 0) {
//            return;
//        }

//        if (selected.size === 0) {
//            $body.html(`
//                <tr class="empty-row">
//                    <td colspan="4" class="selected-empty">
//                        <i class="fa-regular fa-user"></i>
//                        <span>Chưa có nhân viên nào được chọn</span>
//                    </td>
//                </tr>
//            `);

//            return;
//        }

//        let html = "";

//        selected.forEach(function (emp) {
//            html += `
//                <tr>
//                    <td>${escapeHtml(emp.code)}</td>
//                    <td>${escapeHtml(emp.name)}</td>
//                    <td>
//                        <span class="luuyke-badge ${getLuuykeClass(emp.luuyke)}">
//                            ${getLuuykeIcon(emp.luuyke)} ${escapeHtml(emp.luuyke)} giờ
//                        </span>
//                    </td>
//                    <td>
//                        <button type="button"
//                                class="remove-emp"
//                                data-id="${escapeAttr(emp.id)}">
//                            ✕
//                        </button>
//                    </td>
//                </tr>
//            `;
//        });

//        $body.html(html);
//    }

//    // =====================================================
//    // CHECK ALL STATE
//    // =====================================================

//    function updateCheckAllState($dept) {
//        if (!$dept || $dept.length === 0) {
//            return;
//        }

//        const $items = $dept.find(".employee-checkbox");
//        const $checked = $items.filter(":checked");
//        const $checkAll = $dept.find(".check-all-department");

//        const total = $items.length;
//        const checked = $checked.length;

//        if (total === 0) {
//            $checkAll.prop("checked", false).prop("indeterminate", false);
//            return;
//        }

//        if (checked === total) {
//            $checkAll.prop("checked", true).prop("indeterminate", false);
//        } else if (checked === 0) {
//            $checkAll.prop("checked", false).prop("indeterminate", false);
//        } else {
//            $checkAll.prop("checked", false).prop("indeterminate", true);
//        }
//    }

//    // =====================================================
//    // CONFIRM USER FILTER
//    // =====================================================

//    function getSelectedDepartments() {
//        const departments = [];

//        selected.forEach(function (emp) {
//            const dept = (emp.dept || "").toString();

//            if (dept && departments.indexOf(dept) < 0) {
//                departments.push(dept);
//            }
//        });

//        return departments;
//    }

//    function syncConfirmUsers() {
//        if ($confirmSelect.length === 0) {
//            return;
//        }

//        const selectedDepartments = getSelectedDepartments();

//        if (selectedDepartments.length === 0) {
//            resetConfirmUsers();
//            return;
//        }

//        const currentValue = $confirmSelect.val();

//        let currentStillValid = false;
//        let visibleCount = 0;

//        $confirmSelect.find("option").each(function () {
//            const $option = $(this);

//            if (!$option.val()) {
//                $option.prop("disabled", false);
//                $option.prop("hidden", false);
//                return;
//            }

//            const raw = ($option.attr("data-depts") || "").toString();

//            const approverDepartments = raw
//                ? raw.split("|").filter(function (x) {
//                    return x;
//                })
//                : [];

//            const isMatched = selectedDepartments.every(function (dept) {
//                return approverDepartments.indexOf(dept) >= 0;
//            });

//            $option.prop("disabled", !isMatched);
//            $option.prop("hidden", !isMatched);

//            if (isMatched) {
//                visibleCount++;
//            }

//            if (isMatched && $option.val() === currentValue) {
//                currentStillValid = true;
//            }
//        });

//        if (!currentStillValid) {
//            $confirmSelect.val("");
//        }

//        if (visibleCount === 0) {
//            $confirmHint.show();
//        } else {
//            $confirmHint.hide();
//        }

//        refreshConfirmSelect();
//    }

//    function resetConfirmUsers() {
//        if ($confirmSelect.length === 0) {
//            return;
//        }

//        $confirmSelect.val("");

//        $confirmSelect.find("option").each(function () {
//            $(this)
//                .prop("disabled", false)
//                .prop("hidden", false);
//        });

//        $confirmHint.hide();

//        refreshConfirmSelect();
//    }

//    function refreshConfirmSelect() {
//        $confirmSelect.trigger("change");

//        if ($.fn.select2 && $confirmSelect.hasClass("select2-hidden-accessible")) {
//            $confirmSelect.trigger("change.select2");
//        }
//    }

//    // =====================================================
//    // EVENTS - EMPLOYEE CHECKBOX
//    // =====================================================

//    $(document).on("change", ".employee-checkbox", function () {
//        const $el = $(this);
//        const $dept = $el.closest(".dept-content");

//        const emp = getEmployee($el);

//        if (!emp.id) {
//            return;
//        }

//        if ($el.is(":checked")) {
//            selected.set(emp.id, emp);
//        } else {
//            selected.delete(emp.id);
//        }

//        updateCheckAllState($dept);
//        renderSelectedEmployees();
//        syncConfirmUsers();

//        if (selected.size > 0) {
//            $("#empError").fadeOut(100);
//        }
//    });

//    // =====================================================
//    // EVENTS - CHECK ALL DEPARTMENT
//    // =====================================================

//    $(document).on("change", ".check-all-department", function () {
//        const isChecked = $(this).is(":checked");
//        const $dept = $(this).closest(".dept-content");

//        $dept.find(".employee-checkbox").each(function () {
//            const $cb = $(this);
//            const emp = getEmployee($cb);

//            $cb.prop("checked", isChecked);

//            if (!emp.id) {
//                return;
//            }

//            if (isChecked) {
//                selected.set(emp.id, emp);
//            } else {
//                selected.delete(emp.id);
//            }
//        });

//        $(this).prop("indeterminate", false);

//        updateCheckAllState($dept);
//        renderSelectedEmployees();
//        syncConfirmUsers();

//        if (selected.size > 0) {
//            $("#empError").fadeOut(100);
//        }
//    });

//    // =====================================================
//    // EVENTS - REMOVE FROM SELECTED TABLE
//    // =====================================================

//    $(document).on("click", ".remove-emp", function () {
//        const id = ($(this).data("id") || "").toString();

//        if (!id) {
//            return;
//        }

//        selected.delete(id);

//        const $cb = $(".employee-checkbox[value='" + cssEscape(id) + "']");

//        $cb.prop("checked", false);

//        updateCheckAllState($cb.closest(".dept-content"));
//        renderSelectedEmployees();
//        syncConfirmUsers();
//    });

//    // =====================================================
//    // FORM SUBMIT
//    // =====================================================

//    $form.on("submit", function (e) {
//        if (selected.size === 0) {
//            e.preventDefault();

//            $("#empError").fadeIn(150);

//            $("html, body").animate({
//                scrollTop: $(".selected-box").offset().top - 100
//            }, 300);

//            return false;
//        }

//        if ($confirmSelect.length > 0) {
//            const confirmValue = $confirmSelect.val();

//            if (confirmValue) {
//                const $selectedOption = $confirmSelect.find("option:selected");

//                if ($selectedOption.prop("disabled")) {
//                    e.preventDefault();

//                    $confirmSelect.val("");
//                    refreshConfirmSelect();

//                    $confirmHint
//                        .text("Người xác nhận không còn phù hợp với bộ phận đã chọn.")
//                        .show();

//                    return false;
//                }
//            }
//        }

//        $("#empError").fadeOut(100);

//        return true;
//    });

//    // =====================================================
//    // HELPERS
//    // =====================================================

//    function escapeHtml(text) {
//        return $("<div>")
//            .text(text === null || text === undefined ? "" : text)
//            .html();
//    }

//    function escapeAttr(text) {
//        return escapeHtml(text)
//            .replace(/"/g, "&quot;")
//            .replace(/'/g, "&#039;");
//    }

//    function cssEscape(value) {
//        if (window.CSS && CSS.escape) {
//            return CSS.escape(value);
//        }

//        return value.replace(/'/g, "\\'");
//    }
//});

$(function () {
    const $date = $("#DateRequest");
    const $fromTime = $("#FromTime");
    const $toTime = $("#ToTime");
    const $shift = $(".shift-options");
    const $radios = $("input[name='Request.OvertimeType']");

    initMinDate();
    disableShift();

    HRMS.EmployeeConfirmSelector.init({
        formSelector: "#OverTimeForm",
        renderMode: "overtime"
    });

    function initMinDate() {
        const yesterday = new Date();

        yesterday.setDate(yesterday.getDate() - 1);

        const yyyy = yesterday.getFullYear();
        const mm = String(yesterday.getMonth() + 1).padStart(2, "0");
        const dd = String(yesterday.getDate()).padStart(2, "0");

        const minDate = `${yyyy}-${mm}-${dd}`;

        $date.attr("min", minDate);
        $fromTime.attr("min", minDate + "T00:00");
        $toTime.attr("min", minDate + "T00:00");
    }

    function disableShift() {
        $shift.removeClass("active");
        $radios.prop("checked", false);
        $radios.prop("disabled", true);
    }

    function enableShift() {
        $shift.addClass("active");
        $radios.prop("disabled", false);
    }

    function syncDateWithDateTime() {
        const selectedDate = $date.val();

        if (!selectedDate) {
            return;
        }

        const fromValue = $fromTime.val();
        const toValue = $toTime.val();

        const fromTimePart =
            fromValue && fromValue.includes("T")
                ? fromValue.split("T")[1]
                : "16:30";

        const toTimePart =
            toValue && toValue.includes("T")
                ? toValue.split("T")[1]
                : "19:00";

        $fromTime.val(selectedDate + "T" + fromTimePart);
        $toTime.val(selectedDate + "T" + toTimePart);
    }

    function applyHolidayShift(type) {
        const selectedDate = $date.val();

        if (!selectedDate) {
            return;
        }

        if (type === "HanhChinh") {
            $fromTime.val(selectedDate + "T07:50");
            $toTime.val(selectedDate + "T16:30");
            return;
        }

        if (type === "Khac") {
            $fromTime.val(selectedDate + "T16:30");
            $toTime.val(selectedDate + "T19:00");
        }
    }

    function checkDayOff() {
        const date = $date.val();

        if (!date) {
            disableShift();
            return;
        }

        $.ajax({
            url: window.checkDayOffUrl,
            type: "GET",
            data: {
                date: date
            },
            success: function (res) {
                if (!res || !res.success) {
                    disableShift();
                    return;
                }

                if (res.isDayOff) {
                    enableShift();
                    $radios.prop("checked", false);
                } else {
                    disableShift();
                }
            },
            error: function () {
                disableShift();
            }
        });
    }

    $date.on("change", function () {
        syncDateWithDateTime();
        checkDayOff();
    });

    $radios.on("change", function () {
        if ($(this).is(":disabled")) {
            return;
        }

        applyHolidayShift($(this).val());
    });
});