
var venues = [];

function initializeMap() {

    var markers = new ol.layer.Vector({
        source: new ol.source.Vector(),
        style: new ol.style.Style({
          image: new ol.style.Icon({
            anchor: [0.5, 1],
            src: 'img/marker.png'
          })
        })
      });
      
      var marker = new ol.Feature(new ol.geom.Point(ol.proj.fromLonLat([-122.45, 37.75])));
      markers.getSource().addFeature(marker);

    var vectorSource = new ol.source.Vector({
        features: []
    });

    var vectorLayer = new ol.layer.Vector({
        source: vectorSource,
    });

    window.map = new ol.Map({
        target: 'map',
        layers: [
            new ol.layer.Tile({
                source: new ol.source.OSM()
            }),
            vectorLayer
        ],
        view: new ol.View({ 
            center: ol.proj.fromLonLat([-122.45, 37.75]),
            zoom: 13
        })
    });

    window.map.addLayer(markers);


    var dotStyle = new ol.style.Style({
        fill: new ol.style.Fill({
            color: 'rgba(0, 0, 255, 0.2)'
        }),
        image: new ol.style.Icon({
            anchor: [0.5, 0.5],
            anchorXUnits: 'fraction',
            anchorYUnits: 'fraction',
            src: './img/dot.png'
        })
    });
    vectorLayer.setStyle(dotStyle);
}

function updateMap(venue) {

    var loc = new ol.geom.Point(ol.proj.fromLonLat([venue.longitude, venue.latitude]));

    venues[venue.VenueId] = {
        feature: new ol.Feature({ geometry: loc }),
        label: venue.Name
    };

    venues[venue.VenueId].feature.setStyle(dotStyle);
    vectorSource.addFeature(venues[venue.VenueId].feature);
}

function displayMap() {
//     const connection = new signalR.HubConnectionBuilder()
//     .withUrl("./locationHub")
//     .build();

// connection.start().catch(err => console.error);
// connection.on('locationUpdates', data => {
// data.messages.forEach(m =>
// {
// var loc = new ol.geom.Point(ol.proj.fromLonLat([m.longitude, m.latitude]));
// var dev = devices[m.deviceId];
// if (dev) {
// // Ignore old messages unless we have skipped several already (which would indicate that the device gateway was restarted).
// if (dev.messageId < m.messageId || dev.skipped >= 5) {
// dev.feature.setGeometry(loc);
// dev.messageId = m.messageId;
// dev.skipped = 0;
// }
// else {
// dev.skipped++;
// }
// }
// else {
// devices[m.deviceId] = {
// feature: new ol.Feature({ geometry: loc }),
// messageId: m.messageId,
// skipped: 0
// };
// devices[m.deviceId].feature.setStyle(dotStyle);
// vectorSource.addFeature(devices[m.deviceId].feature);
// }
// };

}

  




/////////////////////////

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
  
  var markerStyle = new ol.style.Style({
    image: new ol.style.Icon(({
      src: 'marker.png'
    }))
  });
  
  var textStyle = new ol.style.Style({
    text: new ol.style.Text({
      text: '',
      fill: new ol.style.Fill({
        color: '#000000'
      }),
      stroke: new ol.style.Stroke({
        color: '#FFFFFF',
        width: 3
      })
    })
  });
  
  var markers = [];
  
  function addMarker(lat, lon, text) {
    var coord = ol.proj.fromLonLat([lon, lat]);
  
    var marker = new ol.Feature({
      geometry: new ol.geom.Point(coord),
      name: text
    });
  
    marker.setStyle(markerStyle);
  
    var textFeature = new ol.Feature({
      geometry: new ol.geom.Point(coord),
      name: text
    });
  
    textFeature.setStyle(textStyle);
    textFeature.getStyle().getText().setText(text);
  
    markers.push(marker);
    markers.push(textFeature);
  }
  
  addMarker(52.370216, 4.895168, 'Venue 1');
  addMarker(51.922150, 4.486670, 'Venue 2');
  
  var vectorLayer = new ol.layer.Vector({
    source: new ol.source.Vector({
      features: markers
    })
  });
  
  map.addLayer(vectorLayer);
  
  // Calculate the extent of all markers
  var extent = ol.extent.boundingExtent(markers.map(function (feature) {
    return feature.getGeometry().getCoordinates();
  }));
  
  // Fit the extent to the view
  map.getView().fit(extent, map.getSize());
  