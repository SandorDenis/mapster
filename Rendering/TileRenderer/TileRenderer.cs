using Mapster.Common.MemoryMappedTypes;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Mapster.Rendering;

public static class TileRenderer
{
    public static BaseShape Tessellate(this MapFeatureData feature, ref BoundingBox boundingBox, ref PriorityQueue<BaseShape, int> shapes)
    {
        BaseShape? baseShape = null;
        var featureType = feature.Type;

        if (Border.ShouldBeBorder(feature))
        {
            var coordinates = feature.Coordinates;
            var border = new Border(coordinates);
            baseShape = border;
            shapes.Enqueue(border, border.ZIndex);
        }
        else if (PopulatedPlace.ShouldBePopulatedPlace(feature))
        {
            var coordinates = feature.Coordinates;
            var popPlace = new PopulatedPlace(coordinates, feature);
            baseShape = popPlace;
            shapes.Enqueue(popPlace, popPlace.ZIndex);
        }
        else {
            bool found = false;

            foreach (var ft in feature.Properties) {
                if (found == true) {break;}
                ReadOnlySpan<Coordinate> coordinates;
                switch (ft.Key) {
                    case MapFeatureData.ValueEnum.Highway:
                        if (MapFeature.HighwayTypes.Any(v => ft.Value.StartsWith(v))) {
                            found = true;
                            coordinates = feature.Coordinates;
                            var road = new Road(coordinates);
                            baseShape = road;
                            shapes.Enqueue(road, road.ZIndex);
                        }
                        break;
                    case MapFeatureData.ValueEnum.Water:
                        if (feature.Type != GeometryType.Point) {
                            found = true;
                            coordinates = feature.Coordinates;
                            var waterway = new Waterway(coordinates, feature.Type == GeometryType.Polygon);
                            baseShape = waterway;
                            shapes.Enqueue(waterway, waterway.ZIndex);
                        }
                        break;
                    case MapFeatureData.ValueEnum.Railway:
                        found = true;
                        coordinates = feature.Coordinates;
                        var railway = new Railway(coordinates);
                        baseShape = railway;
                        shapes.Enqueue(railway, railway.ZIndex);
                        break;
                    case MapFeatureData.ValueEnum.Natural:
                        if (featureType == GeometryType.Polygon) {
                            found = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, feature);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }
                        break;
                    case MapFeatureData.ValueEnum.Boundary:
                        if (ft.Value.StartsWith("forest")) {
                            found = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Forest);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }
                        break;
                    case MapFeatureData.ValueEnum.LandType:
                        if (ft.Value.StartsWith("forest") || ft.Value.StartsWith("orchard")) {
                            found = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Forest);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        } else if (feature.Type == GeometryType.Polygon) {
                            if (ft.Value.StartsWith("residential") || ft.Value.StartsWith("cemetery") ||
                                ft.Value.StartsWith("industrial") || ft.Value.StartsWith("commercial") ||
                                ft.Value.StartsWith("square") || ft.Value.StartsWith("construction") ||
                                ft.Value.StartsWith("military") || ft.Value.StartsWith("quarry") ||
                                ft.Value.StartsWith("brownfield")) {
                                found = true;
                                coordinates = feature.Coordinates;
                                var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
                                baseShape = geoFeature;
                                shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                            }else if (ft.Value.StartsWith("farm") || ft.Value.StartsWith("meadow") ||
                                      ft.Value.StartsWith("grass") || ft.Value.StartsWith("greenfield") ||
                                      ft.Value.StartsWith("recreation_ground") || ft.Value.StartsWith("winter_sports")
                                      || ft.Value.StartsWith("allotments")) {
                                found = true;
                                coordinates = feature.Coordinates;
                                var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Plain);
                                baseShape = geoFeature;
                                shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                            } else if (ft.Value.StartsWith("reservoir") || ft.Value.StartsWith("basin")) {
                                found = true;
                                coordinates = feature.Coordinates;
                                var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Water);
                                baseShape = geoFeature;
                                shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                            }
                        }

                        break;
                    case MapFeatureData.ValueEnum.Building:
                        if (feature.Type == GeometryType.Polygon) {
                            found = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }
                        break;
                    case MapFeatureData.ValueEnum.Others:
                        if (feature.Type == GeometryType.Polygon) {
                            found = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }
                        break;
                    case MapFeatureData.ValueEnum.Amenity:
                        if (feature.Type == GeometryType.Polygon) {
                            found = true;
                            coordinates = feature.Coordinates;
                            var geoFeature = new GeoFeature(coordinates, GeoFeature.GeoFeatureType.Residential);
                            baseShape = geoFeature;
                            shapes.Enqueue(geoFeature, geoFeature.ZIndex);
                        }

                        break;
                    default:
                        break;
                }
            }
        }

        if (baseShape != null)
        {
            for (var j = 0; j < baseShape.ScreenCoordinates.Length; ++j)
            {
                boundingBox.MinX = Math.Min(boundingBox.MinX, baseShape.ScreenCoordinates[j].X);
                boundingBox.MaxX = Math.Max(boundingBox.MaxX, baseShape.ScreenCoordinates[j].X);
                boundingBox.MinY = Math.Min(boundingBox.MinY, baseShape.ScreenCoordinates[j].Y);
                boundingBox.MaxY = Math.Max(boundingBox.MaxY, baseShape.ScreenCoordinates[j].Y);
            }
        }

        return baseShape;
    }

    public static Image<Rgba32> Render(this PriorityQueue<BaseShape, int> shapes, BoundingBox boundingBox, int width, int height)
    {
        var canvas = new Image<Rgba32>(width, height);

        // Calculate the scale for each pixel, essentially applying a normalization
        var scaleX = canvas.Width / (boundingBox.MaxX - boundingBox.MinX);
        var scaleY = canvas.Height / (boundingBox.MaxY - boundingBox.MinY);
        var scale = Math.Min(scaleX, scaleY);

        // Background Fill
        canvas.Mutate(x => x.Fill(Color.White));
        while (shapes.Count > 0)
        {
            var entry = shapes.Dequeue();
            // FIXME: Hack
            if (entry.ScreenCoordinates.Length < 2)
            {
                continue;
            }
            entry.TranslateAndScale(boundingBox.MinX, boundingBox.MinY, scale, canvas.Height);
            canvas.Mutate(x => entry.Render(x));
        }

        return canvas;
    }

    public struct BoundingBox
    {
        public float MinX;
        public float MaxX;
        public float MinY;
        public float MaxY;
    }
}
