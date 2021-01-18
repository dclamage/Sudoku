window.getSVG_XY = (svg, x, y) => {
    var pt = svg.createSVGPoint();
    pt.x = x;
    pt.y = y;
    pt = pt.matrixTransform(svg.getScreenCTM().inverse());
    return pt.x + " " + pt.y;
};
window.getBoundingClientRect = (element) => {
    return element.getBoundingClientRect();
};
window.setFocusToElement = (element) => {
    element.focus();
};
window.keypadWidth = 0;
window.keypadHeight = 0;
window.setKeypadSize = (element) => {
    window.keypadWidth = $(element).outerWidth(true);
    window.keypadHeight = $(element).outerHeight(true);
    window.setSidebarSize();
}
window.drawerWidth = 0;
window.drawerHeight = 0;
window.setDrawerSize = (element) => {
    if (element) {
        window.drawerWidth = $(element).outerWidth(true);
        window.drawerHeight = $(element).outerHeight(true);
    } else {
        window.drawerWidth = 0;
        window.drawerHeight = 0;
    }
    window.setSidebarSize();
}
window.appbarHeight = 0;
window.setAppbarHeight = (element) => {
    window.appbarHeight = $(element).outerHeight(true);
    window.setSidebarSize();
}
window.setSidebarSize = () => {
    document.documentElement.style.setProperty(`--sidebar-width`, window.keypadWidth + window.drawerWidth);
    document.documentElement.style.setProperty(`--sidebar-height`, window.keypadHeight + window.drawerHeight + appbarHeight);
}
window.doSaveSvgAsPng = (element, name) => {
    saveSvgAsPng(element, name);
}
window.scrollToBottom = (element) => {
    element.scrollTop = element.scrollHeight;
}

//Prevents backspace except in the case of textareas and text inputs to prevent user navigation.
$(document).keydown(function (e) {
    var preventKeyPress;
    if (e.keyCode == 8 || (e.keyCode == 37 || e.keyCode == 39) && e.altKey) {
        var d = e.srcElement || e.target;
        switch (d.tagName.toUpperCase()) {
            case 'TEXTAREA':
                preventKeyPress = d.readOnly || d.disabled;
                break;
            case 'INPUT':
                preventKeyPress = d.readOnly || d.disabled ||
                    (d.attributes["type"] && $.inArray(d.attributes["type"].value.toLowerCase(), ["radio", "checkbox", "submit", "button"]) >= 0);
                break;
            case 'DIV':
                preventKeyPress = d.readOnly || d.disabled || !(d.attributes["contentEditable"] && d.attributes["contentEditable"].value == "true");
                break;
            default:
                preventKeyPress = true;
                break;
        }
    }
    else {
        preventKeyPress = false;
    }

    if (preventKeyPress) {
        e.preventDefault();
    }
});
