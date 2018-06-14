// Code based on SVG2PNG

var clickedElement = null;

document.addEventListener("mousedown", function(event) {
	// right click
	if (event.button == 2) {
		clickedElement = event.target;
	}
});

document.addEventListener("contextmenu", function(event) {
	var currentElement = event.target;

	// Loop through parents until we find a svg
	while (currentElement) {
		if (currentElement.tagName == "svg") {
			clickedElement = currentElement;
			break;
		} else {
			currentElement = currentElement.parentNode;
		}
	}

	// Default to our target if none were found
	if (!currentElement) {
		currentElement = event.target;
	}
});

chrome.extension.onMessage.addListener(function(request, sender, sendResponse) {
	if (request == "getSVG2ClipboardElement") {
		if (clickedElement.tagName == "svg") {
			// We must restore colors from inherited to inline
			var styles = window.getComputedStyle(clickedElement);
			clickedElement.style.fill = styles.getPropertyValue("fill");
			clickedElement.style.stroke = styles.getPropertyValue("stroke");
		}
		// Pass element along
		sendResponse({value: clickedElement.outerHTML});
	}
});
