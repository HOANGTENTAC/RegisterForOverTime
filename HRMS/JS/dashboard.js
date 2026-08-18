$(function () {

    //=========================
    // SIDEBAR
    //=========================

    if (localStorage.getItem("sidebar") === "collapsed") {

        $(".sidebar").addClass("collapsed");
        $(".main").addClass("expand");

    }

    $(".toggle-sidebar").click(function () {

        if ($(window).width() < 992) {

            $(".sidebar").toggleClass("show");
            return;

        }

        $(".sidebar").toggleClass("collapsed");
        $(".main").toggleClass("expand");

        if ($(".sidebar").hasClass("collapsed")) {

            localStorage.setItem("sidebar", "collapsed");

        }
        else {

            localStorage.removeItem("sidebar");

        }

    });

    //=========================
    // ACTIVE MENU
    //=========================

    var url = window.location.pathname.toLowerCase();

    $(".sidebar .nav-link").each(function () {

        var href = $(this).attr("href");

        if (!href) return;

        if (url.indexOf(href.toLowerCase()) >= 0) {

            $(this).addClass("active");

            $(this)
                .closest(".collapse")
                .addClass("show");

        }

    });

    //=========================
    // TOOLTIP
    //=========================

    $('[data-bs-toggle="tooltip"]').tooltip();

});