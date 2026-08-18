$(function () {

    loadCalendar();

    $('#calendarMonth').on('change', loadCalendar);

    $('#calendarYear').on('change', loadCalendar);
});

function loadCalendar() {

    const month =
        $('#calendarMonth').val();

    const year =
        $('#calendarYear').val();

    $.ajax({

        url: '/Home/GetCalendar',

        type: 'GET',

        data: {
            month: month,
            year: year
        },

        success: function (res) {

            if (!res.success)
                return;

            renderCalendar(
                month,
                year,
                res.data
            );
        },

        error: function () {

            console.error(
                'Không tải được lịch');
        }
    });
}

function renderCalendar(
    month,
    year,
    days) {

    $('#calendarTitle')
        .text(`Tháng ${month} / ${year}`);

    let html = `
        <div class="week">T2</div>
        <div class="week">T3</div>
        <div class="week">T4</div>
        <div class="week">T5</div>
        <div class="week">T6</div>
        <div class="week sat">T7</div>
        <div class="week sun">CN</div>
    `;

    const firstDate =
        new Date(year, month - 1, 1);

    const blankDays =
        (firstDate.getDay() + 6) % 7;

    for (let i = 0; i < blankDays; i++) {

        html += '<div></div>';
    }

    days.forEach(day => {

        let css = 'work';

        if (day.IsOff)
            css = 'off';

        if (day.IsHoliday)
            css = 'holiday';

        if (day.IsToday)
            css += ' today';

        html += `

            <div class="day ${css}">

                <span>
                    ${day.DayNumber}
                </span>

                ${day.IsHoliday
                ? '<small>🎉</small>'
                : ''}

            </div>
        `;
    });

    $('#calendarGrid').html(html);
}