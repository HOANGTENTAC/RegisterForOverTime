$(function () {
    bindEvents();
    function bindEvents() {
        $('.btn-search').on('click', function () {
            doSearch();
        });
        $('#txtSearch').on('keypress', function (e) {

            if (e.which === 13) {
                e.preventDefault();
                doSearch();
            }
        });

        $('.btn-refresh').on('click', function () {
            $('#txtSearch').val('');
            $('#fromDate').val('');
            $('#toDate').val('');
            doSearch();
        });
    }

    function doSearch() {

        const keyword = $('#txtSearch').val().trim();
        const fromDate = $('#fromDate').val();
        const toDate = $('#toDate').val();

        const $table = $('#tblRequest');
        const $tbody = $('#tblRequest tbody');

        $table.addClass('loading');

        $.get('/Home/SearchRequests', {
            keyword: keyword,
            fromDate: fromDate,
            toDate: toDate
        })

            .done(function (res) {
                if (!res.success) {
                    renderNoData();
                    return;
                }
                if (!res.data || res.data.length === 0) {
                    renderNoData();
                    return;
                }

                let html = '';
                res.data.forEach(item => {
                    const typeClass =
                        item.TicketTypeId === 1
                            ? 'ot'
                            : 'leave';
                    const statusClass =
                        item.StatusId === 1
                            ? 'pending'
                            : item.StatusId === 2
                                ? 'approved'
                                : 'rejected';
                    let reason = item.ReasonRequest || '';

                    if (reason.length > 30) {
                        reason = reason.substring(0, 30) + '...';
                    }

                    html += `
                    <tr>
                        <td>
                            <strong>${item.TicketNo}</strong>
                        </td>
                        <td>
                            <span class="type-badge ${typeClass}">
                                ${item.TypeName}
                            </span>
                        </td>
                        <td>
                            ${item.RequestDate}
                        </td>
                        <td title="${escapeHtml(item.ReasonRequest || '')}">
                            ${escapeHtml(reason)}
                        </td>
                        <td>
                            <span class="status-badge ${statusClass}">
                                ${item.StatusName}
                            </span>
                        </td>
                        <td class="text-center">
                            <button
                                class="btn btn-sm btn-outline-primary rounded-circle"
                                style="width:38px;height:38px;"
                                onclick="location.href='/OverTime/Detail?tblTicketId=${item.Id}'">
                                <i class="fas fa-eye"></i>
                            </button>
                        </td>
                    </tr>
                `;
                });

                $tbody.stop(true, true)
                    .fadeOut(120, function () {
                        $tbody.html(html);
                        $('#requestPagination').empty();
                        paginateTable("tblRequest", 5, "requestPagination"
                        );

                        $tbody.fadeIn(180);
                    });
            })

            .fail(function () {
                toastr.error(
                    'Có lỗi xảy ra khi tìm kiếm'
                );
            })

            .always(function () {
                setTimeout(function () {
                    $table.removeClass('loading');

                }, 100);
            });

        function renderNoData() {
            $tbody.html(`
                <tr>
                    <td colspan="6"
                        class="text-center py-4 text-muted">

                        Không có dữ liệu

                    </td>
                </tr>
            `);

            $('#requestPagination').empty();
        }
    }

    function escapeHtml(text) {
        return $('<div>')
            .text(text)
            .html();
    }

});