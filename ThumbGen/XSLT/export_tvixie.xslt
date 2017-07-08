<?xml version="1.0" encoding="UTF-8"?>

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
xmlns:msxsl="urn:schemas-microsoft-com:xslt">

   <xsl:output method="xml" version="1.0" encoding="UTF-8" indent="yes"/>

   <!--xsl:template match="@*|node()">
      <xsl:copy>
         <xsl:apply-templates select="@*|node()"/>
      </xsl:copy>
   </xsl:template-->

	<xsl:template match="movie">
		<xsl:element name="MovieInfo">
			<xsl:element name="Title"><xsl:value-of select="title"/></xsl:element>
			<xsl:element name="OriginalTitle"><xsl:value-of select="originaltitle"/></xsl:element>
			<xsl:element name="Plot"><xsl:value-of select="plot"/></xsl:element>
			<xsl:element name="Year"><xsl:value-of select="year"/></xsl:element>
			<xsl:element name="Runtime"><xsl:value-of select="runtime"/></xsl:element>
			<xsl:element name="Rating"><xsl:value-of select="rating"/></xsl:element>
			<xsl:element name="Certification"><xsl:value-of select="certification"/></xsl:element>
			<xsl:element name="IMDBLink">http://www.imdb.com/title/<xsl:value-of select="id"/>/</xsl:element>
			<xsl:element name="Resolutions"><xsl:value-of select="mediainfo/resolution"/></xsl:element>			
			<xsl:element name="SoundFormats"><xsl:value-of select="mediainfo/audio"/></xsl:element>			
			<xsl:element name="MediaFormats"><xsl:value-of select="mediainfo/format"/></xsl:element>			
			
			<xsl:apply-templates select="genre"/>
			<xsl:apply-templates select="actor"/>
			<xsl:apply-templates select="director"/>
			<xsl:apply-templates select="country"/>
		</xsl:element>
	</xsl:template>

	<xsl:template match="genre">
		<xsl:element name="Genres">
			<xsl:for-each select="name">
				<xsl:element name="string"><xsl:value-of select="."></xsl:value-of></xsl:element>
			</xsl:for-each>
		</xsl:element>
	</xsl:template>

	<xsl:template match="actor">
		<xsl:element name="Actors">
			<xsl:for-each select="name">
				<xsl:element name="string"><xsl:value-of select="."></xsl:value-of></xsl:element>
			</xsl:for-each>
		</xsl:element>
	</xsl:template>

	<xsl:template match="director">
		<xsl:element name="Directors">
			<xsl:for-each select="name">
				<xsl:element name="string"><xsl:value-of select="."></xsl:value-of></xsl:element>
			</xsl:for-each>
		</xsl:element>
	</xsl:template>

	<xsl:template match="country">
		<xsl:element name="Countries">
			<xsl:for-each select="name">
				<xsl:element name="string"><xsl:value-of select="."></xsl:value-of></xsl:element>
			</xsl:for-each>
		</xsl:element>
	</xsl:template>


</xsl:stylesheet>
