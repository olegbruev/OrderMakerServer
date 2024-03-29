﻿const path = require('path');

module.exports = [{
    mode: 'development',
    entry: './wwwroot/lib/mtd-ordermaker/main/scss/mtd-app.scss',
    output: {
        path: __dirname + '/wwwroot/lib/mtd-ordermaker/main',
        filename: 'js/mtd-app.js',
    },
    module: {
        rules: [
            {
                test: /\.scss$/,
                use: [
                    {
                        loader: 'file-loader',
                        options: {                            
                            name: 'css/mtd-app.css',
                        },
                    },
                    { loader: 'extract-loader' },
                    { loader: 'css-loader' },
                    {
                        loader: 'sass-loader',
                        options: {
                            sassOptions: {
                                includePaths: [path.join(path.dirname(module.filename), 'node_modules')
                                ]
                            },
                            // Prefer Dart Sass
                            implementation: require('sass'),

                            // See https://github.com/webpack-contrib/sass-loader/issues/804
                            webpackImporter: false,
                        },                        
                    },
                ]
            }
        ]
    },
}];