    function createCORSRequest(method, url) {
        var xhr = new XMLHttpRequest();
        if ("withCredentials" in xhr) {
            // Check if the XMLHttpRequest object has a "withCredentials" property.
            // "withCredentials" only exists on XMLHTTPRequest2 objects.
            xhr.open(method, url, true);
        } else if (typeof XDomainRequest != "undefined") {
            // Otherwise, check if XDomainRequest.
            // XDomainRequest only exists in IE, and is IE's way of making CORS requests.
            xhr = new XDomainRequest();
            xhr.open(method, url);
        } else {
            // Otherwise, CORS is not supported by the browser.
            xhr = null;
        }

        return xhr;
    }


function gotoPage() {

}

function search(searchTerm) {
    document.getElementById("imgEarthLoading").style.visibility = "visible";
    document.getElementById("searchResults").innerHTML = "SEARCHING...";

    console.log("search = " + searchTerm);
    //  var url = "https://localhost:44300/api/search/" + searchTerm;
    //var url = "https://localhost:44371/api/search/" + searchTerm.toUpperCase();
    var url = "https://localhost:5001/api/search/" + searchTerm.toUpperCase();

    //console.log("search = " + document.getElementById("txtSearch").value)
    //var url = "https://localhost:44371/api/search/" + document.getElementById("txtSearch").value;
    var xhttp = createCORSRequest('GET', url);

    if (!xhttp) {
        throw new Error('CORS not supported');
    }


    var xhr = createCORSRequest('GET', url);

    if (!xhr) {
        alert('CORS not supported');
        return;
    }

    // Response handlers.
    xhr.onload = function () {
        console.log("RespondText = " + xhr.responseText);
        var text = xhr.responseText;

        if (text != '' & text != undefined & text != null) {
            console.log("data found");
            text = text.replace("[", "");
            text = text.replace("]", "");
            text = text.replace('"', "");
            //console.log("text = " + text);


            var dataParts = text.split(':');
            console.log("dataParts[1]=" + dataParts[1]);
            var innerDataParts = dataParts[1].split(',');
            //console.log("innerDataParts[0]=" + innerDataParts[0]);
            //console.log("innerDataParts[1]=" + innerDataParts[1]);
            //console.log("innerDataParts[2]=" + innerDataParts[2]);

            var dataOutput = "";
            var link = "";
            var linkHtml = "";

            for (var iData = 0; iData < innerDataParts.length - 1; iData++) {
                var title = innerDataParts[iData].replace('"', "").replace('"', "").toUpperCase();
                link = "";
                linkHtml = "";

                switch (title) {
                    case "BERKELEY GROUP":
                        link = "berkley.html";
                        break;

                    case "COMPANY 1":
                        link = "company1.html";
                        break;
                }


                if (link != "" & link != undefined & link != null)
                    linkHtml = "<a style='color:white;' onclick='gotoPage(" + link + ");' href='" + link + "'>";

                dataOutput = dataOutput + linkHtml + innerDataParts[iData].replace('"', "").replace('"', "").toUpperCase() + "</a>" + "<br>";
            }

            document.getElementById("searchResults").innerHTML = dataOutput;
            document.getElementById("imgEarthLoading").style.visibility = "hidden";
        } else {
            document.getElementById("searchResults").innerHTML = "Please enter your search query.";
            document.getElementById("imgEarthLoading").style.visibility = "hidden";
        }


        /*
                    var searchResult = JSON.parse(text);
    
                    console.log("searchResult=" + searchResult);
                    console.log("searchResult.searchResult=" + searchResult.searchResult);
                    console.log("searchResult[searchResult]=" + searchResult["searchResult"]);
    
                    var parts = searchResult["searchResult"].split(',');
                    var test2 = searchResult["searchResult"].replace(',',"<br>");
                    console.log("test2 = " + test2);
                    */
        //var parts = searchResult.searchResult.split(',');

        // var str = document.getElementById("demo").innerHTML;
        // var res = str.replace(/blue/g, "red");


        //var parts = searchResult.searchResult.split(',');
        /*
        var test = searchResult.searchResult;
        test = String.replace(",","<br>");
        //searchResult.searchResult = searchResult.searchResult.replace(',',"<br>");
        */




        // var title = getTitle(text);
        // alert('Response from CORS request to ' + url + ': ' + text);
        //document.getElementById("searchResults").innerHTML = searchResult.searchResult
    };

    xhr.onerror = function () {
        //alert('Woops, there was an error making the request.');
        console.log("There was an error making the request.");
        document.getElementById("searchResults").innerHTML = "ERROR OCCURED";
        document.getElementById("imgEarthLoading").style.visibility = "hidden";
    };

    xhr.send();

    /*
                if (window.XMLHttpRequest)
                {
                  // code for modern browsers
                  xhttp = new XMLHttpRequest();
                } else
                {
                  // code for IE6, IE5
                  xhttp = new ActiveXObject("Microsoft.XMLHTTP");
                }
                xhttp.onreadystatechange = function()
                {
                  if (this.readyState == 4 && this.status == 200)
                  {
                    document.getElementById("searchResults").innerHTML = this.responseText;
                  }
                };
    
                xhttp.open("GET", "https://localhost:44371/api/search/" + document.getElementById("txtSearch").value, true);
                xhttp.send();
                */

    //$.post("demo_ajax_gethint.asp", {suggest: txt}, function(result){
    // $.post("https://localhost:44371/api/search/" + document.getElementById("txtSearch").value, {suggest: txt}, function(result){
    //   $("searchResults").html(result);
    // });

    //TODO: Call MongoDB here.
}