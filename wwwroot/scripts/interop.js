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
