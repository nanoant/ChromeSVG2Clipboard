// Code based on SVG2PNG

function genericOnClick(info, tab) {

	chrome.tabs.sendMessage(tab.id, "getClickedEl", function(response) {

		// Get our element raw html
		var elementHTML = response.value;

		// Find <svg> element based on our unique class
		var div = document.createElement("div");
		div.innerHTML = elementHTML;
		var elementSVG = div.firstChild;

		// Verify that we have a <svg> element
		if (elementSVG.tagName.toLowerCase() != "svg") {
			alert("This is not a <svg> element!");
			return;
		}

		// Create an image
		var image = new Image; // Not shown on page
		var svgAsXML = (new XMLSerializer).serializeToString(elementSVG);
		image.src = 'data:image/svg+xml,' + encodeURIComponent(svgAsXML);

		image.onload = function() {
			// Draw our image on canvas
			var canvas = document.createElement('canvas');
			var ctx = canvas.getContext('2d');
			var scale = 2;
			canvas.width = image.width * scale;
			canvas.height = image.height * scale;
			ctx.drawImage(image, 0, 0, image.width * scale, image.height * scale);

			// Copy to clipboard using native messaging host
			if (true) {
				var base64 = canvas.toDataURL().substring('data:image/png;base64,'.length);
				chrome.runtime.sendNativeMessage('com.nanoant.chrome.clipboard',
					{ format: 'PNG', base64: base64 },
					function(response) {
						if (response !== true) {
							alert(response);
						}
					});
			}
			// Download method (as it was originally in SVG2PNG)
			if (false) {
				var a = document.createElement('a');
				a.download = 'figure.png';
				a.href = canvas.toDataURL('image/png');
				document.body.appendChild(a);
				a.addEventListener("click", function(e) {
					a.parentNode.removeChild(a);
				});
				a.click();
			}
			// Copy to clipboard using selection
			if (false) {
				var img = document.createElement('img');
				img.onload = function() {
					var range = document.createRange();
					range.setStartBefore(img);
					range.setEndAfter(img);
					range.selectNode(img);
					var selection = window.getSelection();
					selection.addRange(range);
					document.execCommand('Copy');
					img.parentNode.removeChild(img);
					alert('Copied');
				};
				img.src = canvas.toDataURL();
				document.body.appendChild(img);
			}
			// Copy to clipboard using event.clipboardData
			if (false) {
				var png = atob(canvas.toDataURL().substring('data:image/png;base64,'.length));
				function onCopyHandler(event) {
					event.clipboardData.setData('image/bmp', png);
					event.preventDefault();
					document.removeEventListener('copy', onCopyHandler, true);
				}
				document.addEventListener('copy', onCopyHandler, true);
				document.execCommand('copy');
				alert('Copied');
			}
		};
	});
}

// Create one item to our global context
var context = "all";
var title = "Copy to clipboard as .png";
var id = chrome.contextMenus.create({
	"title": title,
	"contexts":[context],
	"type": "normal",
	"onclick": genericOnClick
});
