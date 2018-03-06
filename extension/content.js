// Code based on SVG2PNG

var clickedEl = null;

document.addEventListener("mousedown", function(event) {
	// right click
	if (event.button == 2) {
		clickedEl = event.target;
	}
});

document.addEventListener("contextmenu", function(event) {
	var currentElement = event.target;

	// Loop through parents until we find a svg
	while (currentElement) {
		if (currentElement.tagName == "svg") {
			clickedEl = currentElement;
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
	if (request == "getClickedEl") {
		// We must restore colors from inherited to inline
		var styles = window.getComputedStyle(clickedEl);
		clickedEl.style.fill = styles.getPropertyValue("fill");
		clickedEl.style.stroke = styles.getPropertyValue("stroke");

		// Pass element along
		sendResponse({value: clickedEl.outerHTML});
	}
});
