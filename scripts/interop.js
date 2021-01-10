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
window.setSidebarSize = (element) => {
    document.documentElement.style.setProperty(`--sidebar-width`, $(element).outerWidth(true));
    document.documentElement.style.setProperty(`--sidebar-height`, $(element).outerHeight(true));
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
