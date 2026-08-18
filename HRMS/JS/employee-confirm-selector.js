window.HRMS = window.HRMS || {};

window.HRMS.EmployeeConfirmSelector = (function () {
    function init(options) {
        const config = $.extend({
            formSelector: "form",
            employeeCheckboxSelector: ".employee-checkbox",
            checkAllSelector: ".check-all-department",
            selectedBodySelector: "#selectedBody",
            empErrorSelector: "#empError",
            confirmSelectSelector: "#ConfirmUserCD",
            confirmHintSelector: "#confirmUserHint",
            renderMode: "overtime",
            shiftTypeSelector: null
        }, options || {});

        const selected = new Map();

        const $form = $(config.formSelector);
        const $confirmSelect = $(config.confirmSelectSelector);
        const $confirmHint = $(config.confirmHintSelector);

        initSelectedEmployees();
        renderSelectedEmployees();
        syncConfirmUsers();

        if ($.fn.select2 && $confirmSelect.length > 0) {
            $confirmSelect.select2({
                width: "100%",
                placeholder: "-- Chọn người xác nhận --"
            });
        }

        $(document).on("change", config.employeeCheckboxSelector, function () {
            const $el = $(this);
            const $dept = $el.closest(".dept-content");
            const emp = getEmployee($el);

            if (!emp.id) {
                return;
            }

            if ($el.is(":checked")) {
                selected.set(emp.id, emp);
            } else {
                selected.delete(emp.id);
            }

            updateCheckAllState($dept);
            renderSelectedEmployees();
            syncConfirmUsers();

            if (selected.size > 0) {
                $(config.empErrorSelector).fadeOut(100);
            }
        });

        $(document).on("change", config.checkAllSelector, function () {
            const isChecked = $(this).is(":checked");
            const $dept = $(this).closest(".dept-content");

            $dept.find(config.employeeCheckboxSelector).each(function () {
                const $cb = $(this);
                const emp = getEmployee($cb);

                $cb.prop("checked", isChecked);

                if (!emp.id) {
                    return;
                }

                if (isChecked) {
                    selected.set(emp.id, emp);
                } else {
                    selected.delete(emp.id);
                }
            });

            $(this).prop("indeterminate", false);

            updateCheckAllState($dept);
            renderSelectedEmployees();
            syncConfirmUsers();

            if (selected.size > 0) {
                $(config.empErrorSelector).fadeOut(100);
            }
        });

        $(document).on("click", ".remove-emp", function () {
            const id = ($(this).data("id") || "").toString();

            if (!id) {
                return;
            }

            selected.delete(id);

            const $cb = $(config.employeeCheckboxSelector).filter(function () {
                return ($(this).val() || "").toString() === id;
            });

            $cb.prop("checked", false);

            updateCheckAllState($cb.closest(".dept-content"));
            renderSelectedEmployees();
            syncConfirmUsers();
        });

        $form.on("submit", function (e) {
            if (selected.size === 0) {
                e.preventDefault();

                $(config.empErrorSelector).fadeIn(150);

                $("html, body").animate({
                    scrollTop: $(".selected-box").offset().top - 100
                }, 300);

                return false;
            }

            if (config.shiftTypeSelector) {
                const $shiftType = $(config.shiftTypeSelector);

                if ($shiftType.length > 0 && !$shiftType.val()) {
                    e.preventDefault();

                    toastr.error("Vui lòng chọn ca làm việc.");

                    $("html, body").animate({
                        scrollTop: $shiftType.offset().top - 120
                    }, 300);

                    return false;
                }
            }

            if ($confirmSelect.length > 0) {
                const confirmValue = $confirmSelect.val();

                if (confirmValue) {
                    const $selectedOption = $confirmSelect.find("option:selected");

                    if ($selectedOption.prop("disabled")) {
                        e.preventDefault();

                        $confirmSelect.val("");
                        refreshConfirmSelect();

                        $confirmHint
                            .text("Người xác nhận không còn phù hợp với bộ phận đã chọn.")
                            .show();

                        return false;
                    }
                }
            }

            $(config.empErrorSelector).fadeOut(100);

            return true;
        });

        function getEmployee($el) {
            return {
                id: ($el.val() || "").toString(),
                code: ($el.data("code") || "").toString(),
                name: ($el.data("name") || "").toString(),
                dept: ($el.data("dept") || "").toString(),
                deptName: getDepartmentName($el),
                luuyke: $el.data("luuyke") || 0
            };
        }

        function getDepartmentName($el) {
            const text = $el
                .closest(".dept-card")
                .find(".dept-header .dept-left span")
                .first()
                .text();

            return (text || "").trim();
        }

        function initSelectedEmployees() {
            $(config.employeeCheckboxSelector + ":checked").each(function () {
                const emp = getEmployee($(this));

                if (emp.id) {
                    selected.set(emp.id, emp);
                }
            });

            $(".dept-content").each(function () {
                updateCheckAllState($(this));
            });
        }

        function renderSelectedEmployees() {
            const $body = $(config.selectedBodySelector);

            if ($body.length === 0) {
                return;
            }

            if (selected.size === 0) {
                $body.html(`
                    <tr class="empty-row">
                        <td colspan="4" class="selected-empty">
                            <i class="fa-regular fa-user"></i>
                            <span>Chưa có nhân viên nào được chọn</span>
                        </td>
                    </tr>
                `);

                return;
            }

            let html = "";

            selected.forEach(function (emp) {
                if (config.renderMode === "shift") {
                    html += renderShiftRow(emp);
                } else {
                    html += renderOvertimeRow(emp);
                }
            });

            $body.html(html);
        }

        function renderOvertimeRow(emp) {
            return `
                <tr>
                    <td>${escapeHtml(emp.code)}</td>
                    <td>${escapeHtml(emp.name)}</td>
                    <td>
                        <span class="luuyke-badge ${getLuuykeClass(emp.luuyke)}">
                            ${getLuuykeIcon(emp.luuyke)} ${escapeHtml(emp.luuyke)} giờ
                        </span>
                    </td>
                    <td>
                        <button type="button"
                                class="remove-emp"
                                data-id="${escapeAttr(emp.id)}">
                            ✕
                        </button>
                    </td>
                </tr>
            `;
        }

        function renderShiftRow(emp) {
            return `
                <tr>
                    <td>${escapeHtml(emp.code)}</td>
                    <td>${escapeHtml(emp.name)}</td>
                    <td>${escapeHtml(emp.deptName)}</td>
                    <td>
                        <button type="button"
                                class="remove-emp"
                                data-id="${escapeAttr(emp.id)}">
                            ✕
                        </button>
                    </td>
                </tr>
            `;
        }

        function getLuuykeClass(val) {
            const h = parseFloat(val || 0);

            if (h >= 40) {
                return "luuyke-danger";
            }

            if (h >= 30) {
                return "luuyke-warning";
            }

            return "luuyke-normal";
        }

        function getLuuykeIcon(val) {
            const h = parseFloat(val || 0);

            if (h >= 40) {
                return "⛔";
            }

            if (h >= 30) {
                return "⚠️";
            }

            return "🟢";
        }

        function updateCheckAllState($dept) {
            if (!$dept || $dept.length === 0) {
                return;
            }

            const $items = $dept.find(config.employeeCheckboxSelector);
            const $checked = $items.filter(":checked");
            const $checkAll = $dept.find(config.checkAllSelector);

            const total = $items.length;
            const checked = $checked.length;

            if (total === 0) {
                $checkAll.prop("checked", false).prop("indeterminate", false);
                return;
            }

            if (checked === total) {
                $checkAll.prop("checked", true).prop("indeterminate", false);
            } else if (checked === 0) {
                $checkAll.prop("checked", false).prop("indeterminate", false);
            } else {
                $checkAll.prop("checked", false).prop("indeterminate", true);
            }
        }

        function getSelectedDepartments() {
            const departments = [];

            selected.forEach(function (emp) {
                const dept = (emp.dept || "").toString();

                if (dept && departments.indexOf(dept) < 0) {
                    departments.push(dept);
                }
            });

            return departments;
        }

        function syncConfirmUsers() {
            if ($confirmSelect.length === 0) {
                return;
            }

            const selectedDepartments = getSelectedDepartments();

            if (selectedDepartments.length === 0) {
                resetConfirmUsers();
                return;
            }

            const currentValue = $confirmSelect.val();

            let currentStillValid = false;
            let visibleCount = 0;

            $confirmSelect.find("option").each(function () {
                const $option = $(this);

                if (!$option.val()) {
                    $option.prop("disabled", false).prop("hidden", false);
                    return;
                }

                const raw = ($option.attr("data-depts") || "").toString();

                const approverDepartments = raw
                    ? raw.split("|").filter(function (x) {
                        return x;
                    })
                    : [];

                const isMatched = selectedDepartments.every(function (dept) {
                    return approverDepartments.indexOf(dept) >= 0;
                });

                $option.prop("disabled", !isMatched);
                $option.prop("hidden", !isMatched);

                if (isMatched) {
                    visibleCount++;
                }

                if (isMatched && $option.val() === currentValue) {
                    currentStillValid = true;
                }
            });

            if (!currentStillValid) {
                $confirmSelect.val("");
            }

            if (visibleCount === 0) {
                $confirmHint
                    .text("Không có người xác nhận phù hợp với bộ phận đã chọn.")
                    .show();
            } else {
                $confirmHint.hide();
            }

            refreshConfirmSelect();
        }

        function resetConfirmUsers() {
            if ($confirmSelect.length === 0) {
                return;
            }

            $confirmSelect.val("");

            $confirmSelect.find("option").each(function () {
                $(this)
                    .prop("disabled", false)
                    .prop("hidden", false);
            });

            $confirmHint.hide();

            refreshConfirmSelect();
        }

        function refreshConfirmSelect() {
            $confirmSelect.trigger("change");

            if ($.fn.select2 && $confirmSelect.hasClass("select2-hidden-accessible")) {
                $confirmSelect.trigger("change.select2");
            }
        }

        function escapeHtml(text) {
            return $("<div>")
                .text(text === null || text === undefined ? "" : text)
                .html();
        }

        function escapeAttr(text) {
            return escapeHtml(text)
                .replace(/"/g, "&quot;")
                .replace(/'/g, "&#039;");
        }
    }

    return {
        init: init
    };
})();