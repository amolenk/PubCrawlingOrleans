var circleRadius = 10;
var circleStrokeWidth = 2;
var circleStrokeColor = '#000';
var circleFillColor = '#fff';

var markers = [];

// Shows the map and draws markers for the venues.
function initializeMap(venues, pageRef) {

    var map = new ol.Map({
        target: 'map',
        layers: [
            new ol.layer.Tile({
                source: new ol.source.OSM()
            })
        ],
        view: new ol.View({
            zoom: 7
        })
    });

    // Add markers to the map
    for (var venue of venues) {
        addMarker(venue.id, venue.name, venue.latitude, venue.longitude, venue.attendance);
    }

    var vectorLayer = new ol.layer.Vector({
        source: new ol.source.Vector({
            features: markers
        })
    });

    map.addLayer(vectorLayer);

    // Register a click handler on the map
    map.on('click', function(evt) {
        var feature = map.forEachFeatureAtPixel(evt.pixel,
          function(feature) {
            if (feature.get('id')) return feature;
          });
        if (feature) {
            var venueId = feature.get('id');
            pageRef.invokeMethodAsync('OnVenueClicked', venueId);
        }
    });

    var extent = ol.extent.boundingExtent(markers.map(function (feature) {
        return feature.getGeometry().getCoordinates();
    }));

    // Create a buffered extent
    var buffer = 1000; // 1km buffer
    var bufferedExtent = ol.extent.buffer(extent, buffer);

    // Fit the buffered extent to the view
    map.getView().fit(bufferedExtent, map.getSize());
}

// Add a marker to the map.
function addMarker(id, name, lat, lon, attendance) {
    var coord = ol.proj.fromLonLat([lon, lat]);

    // Create a style for the circle
    var circleStyle = new ol.style.Style({
        image: new ol.style.Circle({
            radius: 14,
            stroke: new ol.style.Stroke({
                color: circleStrokeColor,
                width: circleStrokeWidth
            }),
            fill: new ol.style.Fill({
                color: circleFillColor
            })
        })
    });

    // Create a style for the venue name text
    var nameTextStyle = new ol.style.Style({
        text: new ol.style.Text({
            text: name,
            font: 'bold 16px Arial',
            fill: new ol.style.Fill({
                color: circleStrokeColor
            }),
            stroke: new ol.style.Stroke({
                color: '#fff',
                width: 3
            }),
            offsetY: 25
        })
    });

    // Create a style for the count text
    var countTextStyle = new ol.style.Style({
        text: new ol.style.Text({
            text: attendance > 0 ? attendance.toString() : '',
            font: 'bold 16px Arial',
            fill: new ol.style.Fill({
                color: circleStrokeColor
            }),
            stroke: new ol.style.Stroke({
                color: '#fff',
                width: 3
            })
        })
    });

    // Create a feature for the marker
    var markerFeature = new ol.Feature({
        geometry: new ol.geom.Point(coord),
        id: id
    });

    // Set the style for the marker
    markerFeature.setStyle([circleStyle, countTextStyle, nameTextStyle]);

    // Add the marker to the markers array
    markers.push(markerFeature);
}

// Update the attendance count on a marker.
function updateMarkerAttendance(venueId, attendance) {

    var markerFeature = markers.find(function (feature) {
        return feature.get('id') === venueId;
    });

    if (markerFeature) {
        // Get the current style of the marker feature
        var style = markerFeature.getStyle();

        // Modify the style as needed
        // For example, update the count text
        var countText = style[1].getText();
        var text = attendance > 0 ? attendance.toString() : '';
        countText.setText(text);

        // Update the style of the marker feature
        markerFeature.setStyle(style);
    }
}
